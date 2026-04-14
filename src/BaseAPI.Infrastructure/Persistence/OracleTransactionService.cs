// !! Result.cs (Clase 1, sección 3) — importamos Result<T>, Result y Error
using BaseAPI.Application.Common;
// !! IUnitOfWorkFactory, IOracleSessionHolder (Clase 1, sección 4) — interfaces de persistencia
using BaseAPI.Application.Contracts.Persistence;
// — Logger para registrar commit/rollback
using Microsoft.Extensions.Logging;

// — Espacio de nombres de Infrastructure/Persistence
namespace BaseAPI.Infrastructure.Persistence;

// — OracleTransactionService: orquesta el flujo completo de una transacción
// !! ITransactionService (Clase 1) — implementamos los 2 métodos ExecuteAsync
public class OracleTransactionService : ITransactionService
{
    // !! IUnitOfWorkFactory (Clase 1 contrato, sección 4 implementación) — para crear UnitOfWork
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    // !! IOracleSessionHolder (Clase 1 contrato, sección 3 implementación) — para registrar la sesión
    private readonly IOracleSessionHolder _sessionHolder;
    // — Logger para registrar commit/rollback y errores
    private readonly ILogger<OracleTransactionService> _logger;

    // — Constructor: recibe las 3 dependencias por DI
    public OracleTransactionService(
        // !! IUnitOfWorkFactory — la fábrica que crea UnitOfWork con conexión abierta
        IUnitOfWorkFactory unitOfWorkFactory,
        // !! IOracleSessionHolder — el holder donde registramos la sesión transaccional
        IOracleSessionHolder sessionHolder,
        // — Logger para registrar los eventos de la transacción
        ILogger<OracleTransactionService> logger)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _sessionHolder = sessionHolder;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // ExecuteAsync<T> — Ejecuta lambda dentro de una transacción (con valor de retorno)
    // ═══════════════════════════════════════════════════════════════
    // !! ITransactionService.ExecuteAsync<T> (Clase 1)
    // — T es el tipo del valor de retorno (ej: string para el resultado de crear estudiante)
    // !! Result<T> (Clase 1) — el lambda retorna Result<T> y este método también retorna Result<T>
    public async Task<Result<T>> ExecuteAsync<T>(
        // — Func<Task<Result<T>>>: un lambda asíncrono que retorna Result<T>
        // — Ejemplo: async () => { ... return Result<string>.Success("OK"); }
        Func<Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        // — PASO 1: Crear UnitOfWork con conexión abierta
        // — "await using": cuando salgamos de este bloque, se llama DisposeAsync automáticamente
        // — Esto GARANTIZA que la conexión se cierre, incluso si hay excepciones
        // !! OracleUnitOfWorkFactory.CreateAsync (sección 4) — crea conexión + UnitOfWork
        await using var unitOfWork = await _unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            // — PASO 2: Iniciar la transacción
            // !! OracleUnitOfWork.BeginTransactionAsync (sección 4) — crea la transacción
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            // — PASO 3: Registrar la sesión en el SessionHolder
            // — A partir de aquí, OracleExecutor detecta HasActiveSession == true
            // — y usa esta conexión compartida para ejecutar los procedures
            // !! OracleSessionHolder.BeginScope (sección 3) — registra conexión/transacción
            // — "using var scope": al salir del bloque, scope.Dispose() limpia la sesión
            using var scope = _sessionHolder.BeginScope(
                unitOfWork.Connection, unitOfWork.Transaction);

            // — PASO 4: Ejecutar el lambda que nos pasaron
            // — Aquí es donde el Handler hace su trabajo real (ej: llamar al repo, ejecutar procedures)
            // — Todas las operaciones del lambda usan la MISMA conexión/transacción
            // !! Result<T> (Clase 1) — el lambda retorna Result<T> con éxito o error
            var result = await operation();

            // — PASO 5: Decidir COMMIT o ROLLBACK según el resultado
            // !! Result.IsSuccess (Clase 1) — si es true → la operación fue exitosa
            if (result.IsSuccess)
            {
                // — COMMIT: los cambios se guardan PERMANENTEMENTE en Oracle
                // !! OracleUnitOfWork.CommitAsync (sección 4)
                await unitOfWork.CommitAsync(cancellationToken);
                _logger.LogDebug("Transacción committed");
            }
            else
            {
                // — ROLLBACK: los cambios se DESHACEN (como si no hubiera pasado nada)
                // !! OracleUnitOfWork.RollbackAsync (sección 4)
                await unitOfWork.RollbackAsync(cancellationToken);
                // !! Result.Error.Message (Clase 1) — el mensaje del error
                _logger.LogDebug("Transacción rollback: {Error}", result.Error.Message);
            }

            // — PASO 6: Retornar el Result<T> (éxito o error) al Handler
            return result;
        }
        catch (Exception ex)
        {
            // — Si el lambda lanza una EXCEPCIÓN (no un Result.Failure, sino un crash real)
            // — Hacemos rollback de emergencia y dejamos que la excepción suba
            // — (el ExceptionHandlingMiddleware de Clase 3 la capturará → HTTP 500)
            _logger.LogError(ex, "Error en transacción. Ejecutando rollback...");
            try
            {
                await unitOfWork.RollbackAsync(cancellationToken);
            }
            catch (Exception rbEx)
            {
                // — Si incluso el rollback falla (conexión perdida), lo registramos
                // — pero NO ocultamos la excepción original
                _logger.LogError(rbEx, "Error adicional durante rollback");
            }
            // — Re-lanzamos la excepción original → sube al Middleware → HTTP 500
            throw;
        }
        // — PASO 7 (automático): al salir de "await using", se llama unitOfWork.DisposeAsync()
        // — Esto cierra la conexión y la devuelve al pool de Oracle
    }

    // ═══════════════════════════════════════════════════════════════
    // ExecuteAsync (sin valor de retorno) — sobrecarga simplificada
    // ═══════════════════════════════════════════════════════════════
    // !! ITransactionService.ExecuteAsync (Clase 1) — versión sin T, solo éxito/fallo
    // — Para operaciones que solo necesitan saber si funcionó o no
    // — Internamente usa la versión con T, envolviendo en Result<bool>
    public async Task<Result> ExecuteAsync(
        // — Lambda que retorna Result (sin valor)
        Func<Task<Result>> operation,
        CancellationToken cancellationToken = default)
    {
        // — Truco: convertimos Result en Result<bool> para reutilizar la lógica de arriba
        var result = await ExecuteAsync(async () =>
        {
            // — Ejecutamos el lambda original que retorna Result
            var r = await operation();
            // — Convertimos a Result<bool>:
            // —   éxito → Result<bool>.Success(true)
            // —   fallo → Result<bool>.Failure(r.Error)
            // !! Result.IsSuccess, Result.Failure (Clase 1) — pattern matching del Result
            return r.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(r.Error);
        }, cancellationToken);

        // — Convertimos de vuelta Result<bool> → Result (sin valor)
        // !! Result.Success(), Result.Failure() (Clase 1) — factory methods estáticos
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}
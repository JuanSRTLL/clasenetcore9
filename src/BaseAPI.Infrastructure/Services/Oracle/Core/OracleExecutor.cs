// — Importamos el sistema de logging para registrar qué procedures se ejecutan
using Microsoft.Extensions.Logging;
// — Importamos las clases del driver de Oracle (OracleCommand, OracleParameter, OracleConnection)
using Oracle.ManagedDataAccess.Client;
// — Importamos System.Data para CommandType (indica que ejecutamos stored procedure)
using System.Data;
// — Importamos las interfaces de Application de las secciones 2.3 y 2.4
// — Referencia: src/BaseAPI.Application/Contracts/Persistence/IDbConnectionFactory.cs
// — Referencia: src/BaseAPI.Application/Contracts/Persistence/IOracleSession.cs
using BaseAPI.Application.Contracts.Persistence;
// — Importamos las excepciones de Infrastructure de la sección 2.2
// — Referencia: src/BaseAPI.Infrastructure/Exceptions/InfrastructureExceptions.cs
using BaseAPI.Infrastructure.Exceptions;

// — Espacio de nombres de este archivo
namespace BaseAPI.Infrastructure.Services.Oracle.Core;

// ═══════════════════════════════════════════════════════════════
// INTERFAZ: define QUÉ puede hacer el ejecutor (sin decir CÓMO)
// ═══════════════════════════════════════════════════════════════
// — Creamos la interfaz IOracleExecutor para que otros archivos dependan del contrato, no de la clase concreta
// —
// — ¿Por qué una interfaz y no usar la clase directamente?
// — Ejemplo: un Repository NO escribe "private readonly OracleExecutor _executor;"
// — sino "private readonly IOracleExecutor _executor;"
// — Así el Repository depende del CONTRATO (qué métodos existen), no de la clase concreta.
// — Si mañana creamos un MockExecutor para pruebas, solo implementa IOracleExecutor
// — y el Repository funciona igual sin cambiar ni una línea.
public interface IOracleExecutor
{
    // — Método para ejecutar un procedure que retorna un cursor (consulta SELECT)
    // — Retorna una lista de objetos T (ej: List<EstudianteDto>)
    // —
    // — "where T : class, new()" — misma restricción que en OracleMapper.MapAsync<T>:
    // —   class  → T debe ser una clase (no int, bool, etc.)
    // —   new()  → T debe tener constructor vacío (para crear instancias con "new T()")
    // — La diferencia: en OracleMapper se necesita para crear los objetos al mapear filas,
    // — aquí se necesita porque ExecuteCursorAsync LLAMA a MapAsync<T> internamente,
    // — y MapAsync<T> exige estas restricciones — si no las pedimos aquí, no compila.
    Task<List<T>> ExecuteCursorAsync<T>(
        string packageName, string procedureName,
        OracleParameter[] parameters,
        // — CancellationToken: igual que en MapAsync, permite cancelar si la operación tarda mucho.
        // — "= default" significa que es opcional — si no lo pasas, se usa CancellationToken.None.
        CancellationToken cancellationToken = default) where T : class, new();

    // — Método para ejecutar un procedure con parámetro de salida (INSERT/UPDATE)
    // — Retorna el valor del parámetro de salida como string (ej: "OK" o "ERROR: duplicado")
    // — No necesita "where T : class, new()" porque no retorna objetos mapeados, solo un string
    Task<string?> ExecuteWithOutputAsync(
        string packageName, string procedureName,
        OracleParameter[] parameters, string outputParameterName,
        CancellationToken cancellationToken = default);
}

// ═══════════════════════════════════════════════════════════════
// IMPLEMENTACIÓN: el motor que realmente ejecuta los procedures
// ═══════════════════════════════════════════════════════════════
// — La clase OracleExecutor implementa la interfaz IOracleExecutor
public class OracleExecutor : IOracleExecutor
{
    // !! IDbConnectionFactory — se usa en GetConnectionContextAsync para obtener conexiones
    private readonly IDbConnectionFactory _connectionFactory;
    // !! IOracleSession — se usa en GetConnectionContextAsync para saber si hay transacción
    private readonly IOracleSession _session;
    // !! OracleMapper  — se usa en ExecuteCursorAsync para convertir filas en objetos C#
    private readonly OracleMapper _mapper;
    // — Logger: registra mensajes de depuración e información
    private readonly ILogger<OracleExecutor> _logger;

    // — Constructor: recibe todas las dependencias por inyección de dependencias
    // — .NET las inyecta automáticamente cuando están registradas en el contenedor DI
    public OracleExecutor(
        IDbConnectionFactory connectionFactory,
        IOracleSession session,
        OracleMapper mapper,
        ILogger<OracleExecutor> logger)
    {
        // — Guardamos cada dependencia en su campo privado para usarla después
        _connectionFactory = connectionFactory;
        _session = session;
        _mapper = mapper;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // ExecuteCursorAsync — Ejecuta procedure que RETORNA DATOS (SELECT)
    // ═══════════════════════════════════════════════════════════════
    // — Ejemplo: PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES retorna un cursor con filas de estudiantes
    public async Task<List<T>> ExecuteCursorAsync<T>(
        string packageName, string procedureName,
        OracleParameter[] parameters,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        // — Construimos el nombre completo del procedure: "PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES"
        var procedureFullName = $"{packageName}.{procedureName}";

        try
        {
            // — Registramos en el log qué procedure vamos a ejecutar
            _logger.LogDebug("Ejecutando procedure con cursor: {Procedure}", procedureFullName);

            // !! IDbConnectionFactory  + IOracleSession  — se usan aquí adentro
            // — Obtenemos conexión, transacción y si debemos cerrar la conexión después
            var (connection, transaction, shouldDispose) = await GetConnectionContextAsync(cancellationToken);

            try
            {
                // — Creamos el comando Oracle apuntando al procedure
                // — "await using" asegura que el comando se destruya al terminar (libera recursos)
                await using var command = new OracleCommand(procedureFullName, connection)
                {
                    // — CommandType.StoredProcedure indica que ejecutamos un procedure, no SQL directo
                    CommandType = CommandType.StoredProcedure,
                    // — BindByName = true: los parámetros se vinculan por NOMBRE, no por posición
                    BindByName = true,
                    // — Si hay transacción activa, la asociamos al comando
                    Transaction = transaction
                };

                // — Si hay parámetros (ej: P_PROGRAMA, P_CURSOR), los agregamos al comando
                if (parameters?.Length > 0)
                    command.Parameters.AddRange(parameters);

                // — Ejecutamos el procedure y obtenemos un reader para leer las filas resultantes
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                // !! OracleMapper  — AQUÍ se usa: convierte las filas del reader en objetos C#
                var results = await _mapper.MapAsync<T>(reader, cancellationToken);

                // — Registramos en el log cuántos registros retornó el procedure
                _logger.LogInformation("Procedure {Procedure} ejecutado. Registros: {Count}",
                    procedureFullName, results.Count);

                // — Retornamos la lista de objetos mapeados
                return results;
            }
            finally
            {
                // — Si la conexión fue creada por nosotros (no de sesión transaccional), la cerramos
                // — Si es de sesión, NO la cerramos porque otros procedures la pueden necesitar
                if (shouldDispose) await connection.DisposeAsync();
            }
        }
        // — Si Oracle lanza un error específico (OracleException)
        catch (OracleException oracleEx)
        {
            // — Registramos el error completo en el log
            _logger.LogError(oracleEx, "Error Oracle en {Procedure}: {Message}",
                procedureFullName, oracleEx.Message);
            // !! OracleProcedureException — AQUÍ se usa: envuelve el error de Oracle
            throw new OracleProcedureException(procedureFullName, oracleEx);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // ExecuteWithOutputAsync — Ejecuta procedure con PARÁMETRO DE SALIDA
    // ═══════════════════════════════════════════════════════════════
    // — Ejemplo: PAQ_ESTUDIANTES.PRO_CREAR_ESTUDIANTE con P_RESULTADO OUT VARCHAR2
    // — El procedure hace un INSERT y retorna "OK" o "ERROR: El estudiante ya existe"
    public async Task<string?> ExecuteWithOutputAsync(
        string packageName, string procedureName,
        OracleParameter[] parameters, string outputParameterName,
        CancellationToken cancellationToken = default)
    {
        // — Construimos el nombre completo: "PAQ_ESTUDIANTES.PRO_CREAR_ESTUDIANTE"
        var procedureFullName = $"{packageName}.{procedureName}";

        try
        {
            // — Registramos en el log qué procedure vamos a ejecutar
            _logger.LogDebug("Ejecutando procedure con output: {Procedure}", procedureFullName);

            // !! IDbConnectionFactory  + IOracleSession  — se usan aquí adentro
            // — Obtenemos conexión, transacción y si debemos cerrar después
            var (connection, transaction, shouldDispose) = await GetConnectionContextAsync(cancellationToken);

            try
            {
                // — Creamos el comando Oracle igual que en ExecuteCursorAsync
                await using var command = new OracleCommand(procedureFullName, connection)
                {
                    // — Indicamos que es un stored procedure
                    CommandType = CommandType.StoredProcedure,
                    // — Vinculamos parámetros por nombre
                    BindByName = true,
                    // — Asociamos la transacción si existe
                    Transaction = transaction
                };

                // — Agregamos los parámetros al comando (incluyendo el parámetro de salida)
                if (parameters?.Length > 0)
                    command.Parameters.AddRange(parameters);

                // — ExecuteNonQuery: ejecutamos el procedure SIN esperar filas de retorno
                // — (los datos de salida vienen en el parámetro OUT, no en un cursor)
                await command.ExecuteNonQueryAsync(cancellationToken);

                // — Buscamos el parámetro de salida por su nombre (ej: "P_RESULTADO")
                var outputParam = command.Parameters.Cast<OracleParameter>()
                    .FirstOrDefault(p => p.ParameterName == outputParameterName);

                // — Extraemos el valor del parámetro de salida como string
                var result = outputParam?.Value?.ToString();
                // — Registramos el resultado en el log
                _logger.LogInformation("Procedure {Procedure} ejecutado. Resultado: {Result}",
                    procedureFullName, result);

                // — Retornamos el resultado (ej: "OK" o "ERROR: duplicado")
                return result;
            }
            finally
            {
                // — Cerramos la conexión solo si fue creada por nosotros
                if (shouldDispose) await connection.DisposeAsync();
            }
        }
        catch (OracleException oracleEx)
        {
            // — Registramos y lanzamos excepción personalizada
            _logger.LogError(oracleEx, "Error Oracle en {Procedure}: {Message}",
                procedureFullName, oracleEx.Message);
            // !! OracleProcedureException (sección 2.2) — AQUÍ se usa: envuelve el error de Oracle
            throw new OracleProcedureException(procedureFullName, oracleEx);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTODO PRIVADO: decide qué conexión usar
    // ═══════════════════════════════════════════════════════════════
    // — Si hay sesión transaccional activa → usamos esa conexión (shouldDispose=false)
    // — Si NO hay sesión → creamos nueva conexión (shouldDispose=true, la cerramos al terminar)
    private async Task<(OracleConnection connection, OracleTransaction? transaction, bool shouldDispose)>
        GetConnectionContextAsync(CancellationToken cancellationToken)
    {
        // !! IOracleSession (sección 2.4) — AQUÍ se usa: preguntamos si hay sesión transaccional
        if (_session.HasActiveSession && _session.CurrentConnection != null)
        {
            // — SÍ hay sesión: usamos su conexión y transacción compartidas
            // — shouldDispose=false: NO cerramos la conexión, la sesión la maneja
            return (
                (OracleConnection)_session.CurrentConnection,
                _session.CurrentTransaction as OracleTransaction,
                shouldDispose: false);
        }

        // !! IDbConnectionFactory (sección 2.3) — AQUÍ se usa: creamos una conexión nueva
        // — shouldDispose=true: SÍ la cerramos nosotros cuando terminemos
        var connection = (OracleConnection)await _connectionFactory.GetOpenConnectionAsync(cancellationToken);
        return (connection, null, shouldDispose: true);
    }
}

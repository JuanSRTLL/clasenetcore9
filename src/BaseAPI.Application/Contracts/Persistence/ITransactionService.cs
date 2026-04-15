// !! Result.cs (Application/Common) — importamos Result<T> y Result
// — Referencia: src/BaseAPI.Application/Common/Result.cs
using BaseAPI.Application.Common;

// — Espacio de nombres de contratos de persistencia
namespace BaseAPI.Application.Contracts.Persistence;

// — Interfaz: Servicio de transacciones simplificado
// — Recibe un lambda (función anónima) y se encarga de:
// —   Crear conexión → Begin transacción → Ejecutar lambda → Commit o Rollback
public interface ITransactionService
{
    // — Versión CON valor de retorno: para operaciones que retornan datos
    // — Ejemplo: crear estudiante y retornar su ID
    // — Func<Task<Result<T>>> es un lambda asíncrono que retorna Result<T>
    Task<Result<T>> ExecuteAsync<T>(
        Func<Task<Result<T>>> operation,
        CancellationToken cancellationToken = default);

    // — Versión SIN valor de retorno: para operaciones que solo necesitan éxito/fallo
    // — Ejemplo: actualizar datos y solo saber si funcionó
    Task<Result> ExecuteAsync(
        Func<Task<Result>> operation,
        CancellationToken cancellationToken = default);
}

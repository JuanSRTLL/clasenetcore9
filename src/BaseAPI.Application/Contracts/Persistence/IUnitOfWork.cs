// — Importamos DbConnection y DbTransaction
using System.Data.Common;

// — Espacio de nombres de contratos de persistencia
namespace BaseAPI.Application.Contracts.Persistence;

// — Interfaz: Unidad de Trabajo — conexión + transacción atómica
// — IAsyncDisposable: se limpia automáticamente al salir del bloque "await using"
public interface IUnitOfWork : IAsyncDisposable
{
    // — La conexión a Oracle que usa esta unidad de trabajo
    DbConnection Connection { get; }
    // — La transacción activa (null antes de llamar a BeginTransactionAsync)
    DbTransaction? Transaction { get; }

    // — Inicia una transacción: a partir de aquí los cambios quedan "pendientes"
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    // — COMMIT: confirma y guarda permanentemente todos los cambios pendientes
    Task CommitAsync(CancellationToken cancellationToken = default);

    // — ROLLBACK: deshace todos los cambios pendientes (como si no hubiera pasado nada)
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

// — Fábrica que crea instancias de IUnitOfWork ya con una conexión abierta lista para usar
public interface IUnitOfWorkFactory
{
    // — Crea un nuevo UnitOfWork con conexión abierta
    Task<IUnitOfWork> CreateAsync(CancellationToken cancellationToken = default);
}

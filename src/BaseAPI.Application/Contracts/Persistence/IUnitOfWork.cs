// — Importamos DbConnection y DbTransaction de System.Data.Common
// — System.Data.Common es un namespace de .NET (no es un NuGet externo)
// — DbConnection es la clase ABSTRACTA genérica para conexiones a CUALQUIER BD
// — DbTransaction es la clase ABSTRACTA genérica para transacciones en CUALQUIER BD
// — Usamos estas clases abstractas (no OracleConnection) para no depender de Oracle
using System.Data.Common;

// — Espacio de nombres de contratos de persistencia
namespace BaseAPI.Application.Contracts.Persistence;

// — Interfaz: Unidad de Trabajo — conexión + transacción atómica
// — IAsyncDisposable: al salir del bloque "await using", se llama automáticamente
// —   a DisposeAsync() que cierra la conexión a BD y libera recursos
// —   Si NO implementáramos IAsyncDisposable, tendríamos que cerrar la conexión
// —   manualmente con try/finally, y si se nos olvida → conexiones Oracle abiertas "colgadas"
public interface IUnitOfWork : IAsyncDisposable
{
    // — La conexión a BD (abstracta: DbConnection, no OracleConnection)
    // — En Clase 4, la implementación real será OracleConnection
    DbConnection Connection { get; }
    // — La transacción activa (null antes de llamar a BeginTransactionAsync)
    // — DbTransaction? = puede ser null (el ? indica nullable)
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
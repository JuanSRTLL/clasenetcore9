// — Importamos DbConnection y DbTransaction de System.Data.Common
using System.Data.Common;
// — Importamos IConfiguration para leer el connection string de appsettings.json
using Microsoft.Extensions.Configuration;
// — Importamos las interfaces que definimos en Clase 1
// !! IUnitOfWork (Clase 1) — la interfaz que OracleUnitOfWork implementa
// !! IUnitOfWorkFactory (Clase 1) — la interfaz que OracleUnitOfWorkFactory implementa
using BaseAPI.Application.Contracts.Persistence;
// — Importamos OracleConnection: la clase concreta del driver Oracle
using Oracle.ManagedDataAccess.Client;

// — Espacio de nombres de Infrastructure/Persistence
namespace BaseAPI.Infrastructure.Persistence;

// ═══════════════════════════════════════════════════════════════
// OracleUnitOfWork — Conexión + Transacción con COMMIT/ROLLBACK
// ═══════════════════════════════════════════════════════════════
// !! IUnitOfWork (Clase 1) — implementamos: Connection, Transaction,
// !!   BeginTransactionAsync, CommitAsync, RollbackAsync, DisposeAsync
public class OracleUnitOfWork : IUnitOfWork
{
    // !! IUnitOfWork.Connection (Clase 1) — la conexión abierta a Oracle
    // — Connection es de tipo DbConnection (genérico) para no acoplarnos a Oracle
    public DbConnection Connection { get; }
    // !! IUnitOfWork.Transaction (Clase 1) — la transacción activa (null antes de BeginTransaction)
    // — "private set": solo esta clase puede cambiar el valor de Transaction
    // — Desde afuera solo se puede leer (get), no asignar
    public DbTransaction? Transaction { get; private set; }

    // — Constructor: recibe una conexión YA ABIERTA (la fábrica la abre)
    // — No abrimos la conexión aquí para separar responsabilidades:
    // —   La fábrica se encarga de CREAR y ABRIR
    // —   El UnitOfWork se encarga de USAR (transacción + commit/rollback)
    public OracleUnitOfWork(DbConnection connection)
    {
        // — Guardamos la conexión que nos dieron
        Connection = connection;
    }

    // ═══════════════════════════════════════════════════════════════
    // BeginTransactionAsync — Inicia la transacción
    // ═══════════════════════════════════════════════════════════════
    // !! IUnitOfWork.BeginTransactionAsync (Clase 1)
    // — A partir de aquí, TODOS los cambios quedan "pendientes"
    // — No se guardan en disco hasta que se haga Commit
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // — BeginTransactionAsync(): Oracle crea una transacción sobre la conexión
        // — Todos los comandos ejecutados después usarán esta transacción
        Transaction = await Connection.BeginTransactionAsync(cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════
    // CommitAsync — Confirma TODOS los cambios de la transacción
    // ═══════════════════════════════════════════════════════════════
    // !! IUnitOfWork.CommitAsync (Clase 1)
    // — Los cambios pendientes se guardan PERMANENTEMENTE en la BD
    // — Ejemplo: si un INSERT fue ejecutado, ahora el registro EXISTE definitivamente
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        // — Solo hacemos commit si hay transacción activa
        if (Transaction != null)
            await Transaction.CommitAsync(cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════
    // RollbackAsync — DESHACE todos los cambios de la transacción
    // ═══════════════════════════════════════════════════════════════
    // !! IUnitOfWork.RollbackAsync (Clase 1)
    // — Es como si el INSERT nunca hubiera pasado
    // — Útil cuando una operación falla y queremos dejar la BD como estaba
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        // — Solo hacemos rollback si hay transacción activa
        if (Transaction != null)
            await Transaction.RollbackAsync(cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════
    // DisposeAsync — Libera conexión y transacción
    // ═══════════════════════════════════════════════════════════════
    // !! IAsyncDisposable (System) — se llama automáticamente al salir de "await using"
    // — Garantiza que SIEMPRE se liberen los recursos, incluso si hay excepciones
    // — Ejemplo:
    // —   await using var uow = await factory.CreateAsync();
    // —   // ... usar uow ...
    // —   // Al salir de este bloque → se llama DisposeAsync automáticamente
    public async ValueTask DisposeAsync()
    {
        // — Primero destruimos la transacción (si existe)
        if (Transaction != null) await Transaction.DisposeAsync();
        // — Luego destruimos la conexión (la devuelve al pool de Oracle)
        await Connection.DisposeAsync();
    }
}

// ═══════════════════════════════════════════════════════════════
// OracleUnitOfWorkFactory — Fábrica que crea UnitOfWork
// ═══════════════════════════════════════════════════════════════
// — ¿Por qué una fábrica y no registrar IUnitOfWork directamente en DI?
// — Porque IUnitOfWork necesita una DbConnection ya abierta en el constructor.
// — DI no sabe cómo crear DbConnection, así que usamos una fábrica que sí sabe.
// — Los Handlers llaman a factory.CreateAsync() para obtener un UnitOfWork listo.
// !! IUnitOfWorkFactory (Clase 1) — implementamos CreateAsync()
public class OracleUnitOfWorkFactory : IUnitOfWorkFactory
{
    // — Connection string de appsettings.json (leído en el constructor)
    private readonly string _connectionString;

    // — Constructor: recibe IConfiguration para leer el connection string
    public OracleUnitOfWorkFactory(IConfiguration configuration)
    {
        // — Leemos el connection string igual que en OracleConnectionFactory
        _connectionString = configuration.GetConnectionString("OracleConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'OracleConnection' no encontrada");
    }

    // ═══════════════════════════════════════════════════════════════
    // CreateAsync — Crea un UnitOfWork con conexión abierta
    // ═══════════════════════════════════════════════════════════════
    // !! IUnitOfWorkFactory.CreateAsync (Clase 1)
    // — Crea una nueva OracleConnection, la abre y la envuelve en un UnitOfWork
    public async Task<IUnitOfWork> CreateAsync(CancellationToken cancellationToken = default)
    {
        // — Creamos la OracleConnection con el connection string
        var connection = new OracleConnection(_connectionString);
        // — Abrimos la conexión (con connection pooling es muy rápido)
        await connection.OpenAsync(cancellationToken);
        // — Creamos y retornamos el UnitOfWork con la conexión abierta
        return new OracleUnitOfWork(connection);
    }
}
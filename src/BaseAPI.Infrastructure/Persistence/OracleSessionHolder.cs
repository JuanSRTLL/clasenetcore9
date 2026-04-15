// — Importamos DbConnection y DbTransaction de System.Data.Common
// — Son tipos genéricos que representan conexión/transacción de cualquier BD
using System.Data.Common;
// — Importamos las interfaces de sesión que definimos en Clase 1
// !! IOracleSessionHolder (Clase 1) — la interfaz que esta clase implementa
// !! IOracleSessionScope (Clase 1) — la interfaz del scope desechable
using BaseAPI.Application.Contracts.Persistence;
// — Logger para registrar inicio/fin de sesiones
using Microsoft.Extensions.Logging;

// — Espacio de nombres de Infrastructure/Persistence
namespace BaseAPI.Infrastructure.Persistence;

// — OracleSessionHolder: mantiene la conexión y transacción compartidas
// — "sealed": esta clase NO se puede heredar (no se necesita extenderla)
// !! IOracleSessionHolder (Clase 1) — implementamos BeginScope() + las propiedades de IOracleSession
public sealed class OracleSessionHolder : IOracleSessionHolder
{
    // — Logger para registrar cuándo se inicia y finaliza la sesión
    private readonly ILogger<OracleSessionHolder> _logger;
    // — La conexión compartida (null si no hay sesión activa)
    // — "?" indica que puede ser null (no siempre hay sesión activa)
    private DbConnection? _connection;
    // — La transacción compartida (null si no hay transacción)
    private DbTransaction? _transaction;

    // — Constructor: solo recibe el logger
    public OracleSessionHolder(ILogger<OracleSessionHolder> logger)
    {
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // Propiedades de IOracleSession (Clase 1)
    // ═══════════════════════════════════════════════════════════════
    // — Estos 3 son los que OracleExecutor lee para decidir qué conexión usar

    // — ¿Hay sesión activa? → true si _connection no es null
    // !! IOracleSession.HasActiveSession (Clase 1)
    public bool HasActiveSession => _connection != null;
    // — La conexión compartida (o null si no hay sesión)
    // !! IOracleSession.CurrentConnection (Clase 1)
    public DbConnection? CurrentConnection => _connection;
    // — La transacción compartida (o null)
    // !! IOracleSession.CurrentTransaction (Clase 1)
    public DbTransaction? CurrentTransaction => _transaction;

    // ═══════════════════════════════════════════════════════════════
    // BeginScope — Inicia un scope transaccional
    // ═══════════════════════════════════════════════════════════════
    // !! IOracleSessionHolder.BeginScope (Clase 1) — el método del contrato
    // — Recibe la conexión y transacción que el UnitOfWork abrió
    // — Retorna un IOracleSessionScope (IDisposable) que limpia al destruirse
    public IOracleSessionScope BeginScope(DbConnection connection, DbTransaction? transaction)
    {
        // — Guardamos la conexión (no puede ser null)
        // — "?? throw" es una validación: si alguien pasa null, falla inmediatamente
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        // — Guardamos la transacción (puede ser null si no se inició transacción)
        _transaction = transaction;
        // — Registramos en el log que la sesión comenzó
        _logger.LogDebug("Sesión Oracle iniciada");
        // — Retornamos un OracleSessionScope que, al hacer Dispose(), llama a EndScope()
        return new OracleSessionScope(this);
    }

    // ═══════════════════════════════════════════════════════════════
    // EndScope — Limpia la sesión (se llama automáticamente)
    // ═══════════════════════════════════════════════════════════════
    // — "private": solo lo puede llamar OracleSessionScope (la clase interna)
    private void EndScope()
    {
        // — Limpiamos la conexión y transacción
        _connection = null;
        _transaction = null;
        // — Registramos que la sesión terminó
        _logger.LogDebug("Sesión Oracle finalizada");
    }

    // ═══════════════════════════════════════════════════════════════
    // OracleSessionScope — Clase interna que limpia al destruirse
    // ═══════════════════════════════════════════════════════════════
    // — "private sealed class": SOLO existe dentro de OracleSessionHolder
    // — Nadie fuera de esta clase puede crear un OracleSessionScope
    // !! IOracleSessionScope (Clase 1) — implementamos End(), Dispose() y DisposeAsync()
    private sealed class OracleSessionScope : IOracleSessionScope
    {
        // — Referencia al OracleSessionHolder que nos creó
        private readonly OracleSessionHolder _holder;
        // — Flag para evitar limpiar dos veces (si llaman End() y luego Dispose())
        private bool _disposed;

        // — Constructor: recibe el holder para poder llamar a EndScope() después
        public OracleSessionScope(OracleSessionHolder holder) => _holder = holder;

        // — End(): finaliza la sesión manualmente (antes de que se destruya el scope)
        // !! IOracleSessionScope.End() (Clase 1)
        public void End()
        {
            // — Si ya se limpió, no hacemos nada (evitar doble limpieza)
            if (_disposed) return;
            // — Marcamos como limpiado
            _disposed = true;
            // — Llamamos a EndScope() del holder para limpiar _connection y _transaction
            _holder.EndScope();
        }

        // — Dispose(): se llama automáticamente al salir del bloque "using"
        // — Patrón IDisposable: garantiza limpieza incluso si hay excepciones
        public void Dispose() => End();
        // — DisposeAsync(): versión asíncrona de Dispose (para "await using")
        public ValueTask DisposeAsync() { End(); return ValueTask.CompletedTask; }
    }
}
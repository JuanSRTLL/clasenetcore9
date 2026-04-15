// — Importamos IConfiguration para leer el connection string de appsettings.json
using Microsoft.Extensions.Configuration;
// — Importamos ILogger para registrar mensajes de depuración y error
using Microsoft.Extensions.Logging;
// — Importamos OracleConnection: la clase del driver Oracle que crea la conexión real
using Oracle.ManagedDataAccess.Client;
// — Importamos las interfaces que definimos en Clase 1 (Application/Contracts/Persistence)
// !! IDbConnectionFactory (Clase 1) — la interfaz que esta clase implementa
using BaseAPI.Application.Contracts.Persistence;
// — Importamos las excepciones de Infrastructure (del repo base, sección 2.2 de Clase 1)
// !! DatabaseConnectionException — se lanza si Oracle no responde (→ HTTP 503 en el Middleware)
using BaseAPI.Infrastructure.Exceptions;

// — Espacio de nombres: todas las clases de persistencia de Infrastructure van aquí
namespace BaseAPI.Infrastructure.Persistence;

// — OracleConnectionFactory: la fábrica que crea conexiones reales a Oracle
// — "Fábrica" = patrón de diseño donde una clase se encarga de CREAR otras cosas
// — En vez de que cada clase cree su propia OracleConnection, le piden a la fábrica
// !! IDbConnectionFactory (Clase 1) — implementamos los 2 métodos del contrato
public class OracleConnectionFactory : IDbConnectionFactory
{
    // — Campo privado: guarda el connection string que leímos de appsettings.json
    // — "readonly": solo se puede asignar en el constructor, después no se puede cambiar
    private readonly string _connectionString;
    // — Logger para registrar mensajes de depuración y error
    private readonly ILogger<OracleConnectionFactory> _logger;
    // !! IOracleSessionHolder (Clase 1) — necesitamos saber si hay sesión transaccional activa
    private readonly IOracleSessionHolder _sessionHolder;

    // — Constructor: recibe las dependencias por inyección de DI
    // — .NET las inyecta automáticamente porque están registradas en el contenedor (sección 6)
    public OracleConnectionFactory(
        // — IConfiguration: da acceso a appsettings.json
        IConfiguration configuration,
        // — ILogger<OracleConnectionFactory>: logger específico para esta clase
        ILogger<OracleConnectionFactory> logger,
        // !! IOracleSessionHolder (Clase 1) — para preguntar si hay sesión activa
        IOracleSessionHolder sessionHolder)
    {
        // — GetConnectionString("OracleConnection"): busca en appsettings.json la clave
        // —   "ConnectionStrings": { "OracleConnection": "User Id=netcore;..." }
        // — ?? throw: si no encuentra el connection string, lanza excepción inmediatamente
        // —   (mejor que recibir NullReferenceException más adelante sin saber por qué)
        _connectionString = configuration.GetConnectionString("OracleConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'OracleConnection' no encontrada en appsettings.json");
        // — Guardamos el logger y el session holder
        _logger = logger;
        _sessionHolder = sessionHolder;
    }

    // ═══════════════════════════════════════════════════════════════
    // CreateConnectionAsync — Crea y abre una conexión NUEVA
    // ═══════════════════════════════════════════════════════════════
    // !! IDbConnectionFactory (Clase 1) — este es el 1er método del contrato
    // — Retorna DbConnection (System.Data.Common) — tipo genérico, no acoplado a Oracle
    public async Task<System.Data.Common.DbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // — Creamos una nueva OracleConnection con el connection string
            // — En este punto la conexión NO está abierta todavía
            var connection = new OracleConnection(_connectionString);
            // — OpenAsync(): abre la conexión contra el servidor Oracle
            // — Si Oracle tiene connection pooling activado (Pooling=true):
            // —   → busca en el pool una conexión libre
            // —   → si hay → la reutiliza (muy rápido, ~1ms)
            // —   → si no hay → crea una nueva (más lento, ~50-200ms)
            await connection.OpenAsync(cancellationToken);
            // — Registramos en el log que la conexión se creó exitosamente
            _logger.LogDebug("Conexión Oracle creada y abierta");
            // — Retornamos la conexión abierta como DbConnection (tipo base)
            return connection;
        }
        // — Si Oracle lanza un error (servidor caído, credenciales incorrectas, red caída)
        catch (OracleException oracleEx)
        {
            // — Registramos el error completo en el log
            _logger.LogError(oracleEx, "Error al conectar con Oracle: {Message}", oracleEx.Message);
            // !! DatabaseConnectionException (repo base, Clase 1 sección 2.2)
            // — Lanzamos nuestra excepción personalizada
            // — El ExceptionHandlingMiddleware (Clase 3) la captura → HTTP 503 Service Unavailable
            throw new DatabaseConnectionException(
                $"Error de conexión a Oracle: {oracleEx.Message}", oracleEx);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // GetOpenConnectionAsync — Obtiene conexión inteligentemente
    // ═══════════════════════════════════════════════════════════════
    // !! IDbConnectionFactory (Clase 1) — este es el 2do método del contrato
    // — Lógica: si hay transacción activa → reutiliza esa conexión
    // —         si no hay transacción → crea una nueva
    public async Task<System.Data.Common.DbConnection> GetOpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        // !! IOracleSessionHolder (Clase 1) — preguntamos si hay sesión transaccional activa
        // — HasActiveSession es true cuando OracleTransactionService (sección 5) inició una transacción
        if (_sessionHolder.HasActiveSession && _sessionHolder.CurrentConnection != null)
        {
            // — SÍ hay sesión activa: reutilizamos su conexión
            // — ¿Por qué? Porque las 2 operaciones deben usar la MISMA conexión
            // — para estar en la MISMA transacción
            _logger.LogDebug("Usando conexión de sesión transaccional activa");
            return _sessionHolder.CurrentConnection;
        }

        // — NO hay sesión activa: creamos una conexión nueva e independiente
        // — Esto pasa en lecturas simples (Queries) que no necesitan transacción
        return await CreateConnectionAsync(cancellationToken);
    }
}
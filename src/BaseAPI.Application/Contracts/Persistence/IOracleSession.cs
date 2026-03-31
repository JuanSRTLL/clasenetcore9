// — Importamos DbConnection y DbTransaction para tipos de conexión y transacción genéricos
using System.Data.Common;

// — Espacio de nombres de contratos de persistencia
namespace BaseAPI.Application.Contracts.Persistence;

// — Interfaz de SOLO LECTURA: permite consultar si hay sesión activa
// — El OracleExecutor usa esta interfaz para decidir qué conexión usar
public interface IOracleSession
{
    // — ¿Hay una sesión transaccional activa en este momento?
    bool HasActiveSession { get; }
    // — La conexión compartida de la sesión actual (null si no hay sesión)
    DbConnection? CurrentConnection { get; }
    // — La transacción compartida de la sesión actual (null si no hay sesión)
    DbTransaction? CurrentTransaction { get; }
}

// — Interfaz EXTENDIDA: permite crear y gestionar sesiones
// — Solo la usa Infrastructure (OracleUnitOfWork) para iniciar una sesión transaccional
public interface IOracleSessionHolder : IOracleSession
{
    // — Inicia un scope de sesión: guarda la conexión y transacción proporcionadas
    // — Retorna un Scope (IDisposable) que al destruirse limpia automáticamente la sesión
    IOracleSessionScope BeginScope(DbConnection connection, DbTransaction? transaction);
}

// — Scope desechable: al salir del bloque "using", se limpia la sesión
// — Esto garantiza que SIEMPRE se limpia, incluso si hay excepciones
public interface IOracleSessionScope : IDisposable, IAsyncDisposable
{
    // — Permite finalizar la sesión manualmente (antes de que se destruya el scope)
    void End();
}

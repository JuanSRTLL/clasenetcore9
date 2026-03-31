// — Importamos las interfaces de excepciones definidas en Application
using BaseAPI.Application.Common.Exceptions;
// — Importamos OracleException del driver de Oracle
using Oracle.ManagedDataAccess.Client;

// — Espacio de nombres para las excepciones de Infrastructure
namespace BaseAPI.Infrastructure.Exceptions;

// ═══════════════════════════════════════════════════════════════
// CLASE BASE ABSTRACTA — todas las excepciones de Infrastructure heredan de aquí
// ═══════════════════════════════════════════════════════════════
// — "abstract" significa que NO se puede instanciar directamente (no hay "new InfrastructureException()")
// — Hereda de Exception (para que .NET la trate como excepción)
// — Implementa IInfrastructureException (para que Application la reconozca sin conocer esta clase)
public abstract class InfrastructureException : Exception, IInfrastructureException
{
    // — Código del error: identifica el tipo de error (ej: "DB_CONNECTION_ERROR")
    public string ErrorCode { get; }

    // — Constructor protegido: solo pueden llamarlo las clases hijas
    // — Recibe un código de error y un mensaje descriptivo
    protected InfrastructureException(string errorCode, string message)
        : base(message) => ErrorCode = errorCode;

    // — Constructor protegido con excepción interna (la excepción original que causó el error)
    protected InfrastructureException(string errorCode, string message, Exception innerException)
        : base(message, innerException) => ErrorCode = errorCode;
}

// ═══════════════════════════════════════════════════════════════
// EXCEPCIÓN: Error de conexión a la base de datos Oracle
// ═══════════════════════════════════════════════════════════════
// — Se lanza cuando no se puede conectar a Oracle (red, servidor caído, credenciales)
// — Implementa IDatabaseConnectionException para que el Middleware sepa que es error de conexión → HTTP 503
public class DatabaseConnectionException : InfrastructureException, IDatabaseConnectionException
{
    // — Constructor: recibe el mensaje de error y opcionalmente la excepción original
    public DatabaseConnectionException(string message, Exception? innerException = null)
        : base("DB_CONNECTION_ERROR", message, innerException ?? new Exception()) { }
}

// ═══════════════════════════════════════════════════════════════
// EXCEPCIÓN: Error al ejecutar un procedimiento almacenado de Oracle
// ═══════════════════════════════════════════════════════════════
// — Se lanza cuando un procedure falla (error de SQL, parámetros incorrectos, etc.)
// — Guarda el nombre del procedure para diagnóstico
public class OracleProcedureException : InfrastructureException, IProcedureException
{
    // — Nombre del procedure que falló (ej: "PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES")
    public string ProcedureName { get; }

    // — Constructor cuando la causa es una OracleException (error del driver Oracle)
    public OracleProcedureException(string procedureName, OracleException oracleException)
        : base("ORACLE_PROCEDURE_ERROR",
               $"Error en procedimiento '{procedureName}': {oracleException.Message}",
               oracleException)
    {
        // — Guardamos el nombre del procedure para incluirlo en los logs
        ProcedureName = procedureName;
    }

    // — Constructor cuando el error es un mensaje personalizado (no OracleException)
    public OracleProcedureException(string procedureName, string message)
        : base("ORACLE_PROCEDURE_ERROR", $"Error en procedimiento '{procedureName}': {message}")
    {
        // — Guardamos el nombre del procedure
        ProcedureName = procedureName;
    }
}

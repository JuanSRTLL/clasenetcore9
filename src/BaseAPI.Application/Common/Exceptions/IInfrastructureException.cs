// — Espacio de nombres para contratos de excepciones
namespace BaseAPI.Application.Common.Exceptions;

// — Interfaz BASE: cualquier error que venga de Infrastructure (Oracle, red, disco, etc.)
// — La capa API usará esta interfaz para atrapar errores sin conocer las clases concretas
public interface IInfrastructureException
{
    // — Código que identifica el tipo de error (ej: "DB_CONNECTION_ERROR", "ORACLE_PROCEDURE_ERROR")
    string ErrorCode { get; }
    // — Mensaje descriptivo del error (ej: "No se pudo conectar a Oracle en 192.168.20.52")
    string Message { get; }
}

// — Interfaz para errores de procedures: hereda de IInfrastructureException
// — Agrega el nombre del procedure que falló para diagnóstico
public interface IProcedureException : IInfrastructureException
{
    // — Nombre completo del procedure que falló (ej: "PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES")
    string ProcedureName { get; }
}

// — Interfaz para errores de conexión: hereda de IInfrastructureException
// — No agrega propiedades extras, pero sirve para identificar el TIPO de error
// — El Middleware de API hará: catch (IDatabaseConnectionException) → HTTP 503
public interface IDatabaseConnectionException : IInfrastructureException { }

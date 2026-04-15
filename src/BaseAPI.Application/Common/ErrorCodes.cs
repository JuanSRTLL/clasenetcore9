// — Definimos el espacio de nombres donde vive este archivo
namespace BaseAPI.Application.Common;

// — "static class" significa que esta clase NO se puede instanciar
// — No se hace "new ErrorCodes()", se usa directamente: ErrorCodes.NotFound
public static class ErrorCodes
{
    // — Recurso no encontrado: cuando buscamos un estudiante y no existe
    // — En la capa API se convertirá en HTTP 404
    public const string NotFound = "NOT_FOUND";

    // — Error de validación: cuando los datos enviados son incorrectos
    // — Ejemplo: nombre vacío, email con formato inválido
    // — En la capa API se convertirá en HTTP 400
    public const string Validation = "VALIDATION";

    // — Error interno del servidor: algo inesperado falló
    // — En la capa API se convertirá en HTTP 500
    public const string InternalError = "INTERNAL_ERROR";

    // — Conflicto: el dato ya existe o hay un problema con el estado actual
    // — Ejemplo: intentar crear un estudiante con un código que ya existe
    // — En la capa API se convertirá en HTTP 409
    public const string Conflict = "CONFLICT";
}

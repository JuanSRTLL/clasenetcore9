using Microsoft.AspNetCore.Mvc;   // — IActionResult, OkObjectResult, NotFoundObjectResult, etc. (las respuestas HTTP)
using BaseAPI.Application.Common; // — Result<T>, Error, ErrorCodes (vienen de la capa Application)
using BaseAPI.API.Models;         // — ApiResponse<T> (el formato JSON que envolvemos en cada respuesta)

namespace BaseAPI.API.Extensions;

// — static class porque solo contiene extension methods (no se instancia).
// —
// — ¿QUÉ PROBLEMA RESUELVE?
// — Sin esto, en cada endpoint del Controller tendríamos que escribir:
// —   if (result.IsSuccess) return Ok(...); else if (error == "NOT_FOUND") return NotFound(...); ...
// — Con esto, solo escribimos: return result.ToActionResult();
// — Y la conversión Result → HTTP Status se hace aquí automáticamente.
public static class ResultExtensions
{
    // — this Result<T> result: extension method sobre Result<T>.
    // — Permite llamar result.ToActionResult() como si fuera un método de Result<T>.
    // —
    // — IActionResult: interfaz de ASP.NET que representa cualquier respuesta HTTP (200, 400, 404, etc.)
    // — El Controller retorna IActionResult y ASP.NET lo convierte en la respuesta HTTP final.
    // —
    // — string? successMessage: el "?" significa que es opcional (puede ser null).
    // — Si no pasas mensaje, usa "Operación exitosa" por defecto.
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        string? successMessage = null)
    {
        // — result.IsSuccess: propiedad de Result<T> que indica si el Handler retornó éxito.
        // — Si es true, el Handler procesó bien → retornamos HTTP 200 OK.
        if (result.IsSuccess)
        {
            // — OkObjectResult: clase de ASP.NET que genera una respuesta HTTP 200 OK.
            // — Recibe un objeto que se serializa a JSON automáticamente.
            // —
            // — ApiResponse<T>.Success(result.Value!, ...): envuelve los datos en nuestro formato estándar.
            // — result.Value!: el dato real (ej: List<EstudianteDto>). El "!" le dice al compilador "confía, no es null".
            // —
            // — successMessage ?? "Operación exitosa": si successMessage es null, usa el texto por defecto.
            // — ?? es el operador "null-coalescing": "usa lo de la izquierda, pero si es null, usa lo de la derecha".
            // —
            // — Resultado JSON:
            // — { "exitoso": true, "mensaje": "Operación exitosa", "datos": [...], "timestamp": "..." }
            return new OkObjectResult(
                ApiResponse<T>.Success(result.Value!, successMessage ?? "Operación exitosa"));
        }

        // — Si no es éxito → tiene un Error. Lo pasamos a MapErrorToActionResult
        // — que decide qué código HTTP retornar según error.Code.
        return MapErrorToActionResult<T>(result.Error);
    }

    // — Igual que ToActionResult pero retorna HTTP 201 Created en vez de 200 OK.
    // — Se usa para endpoints POST que crean recursos nuevos.
    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            // — ObjectResult: clase genérica de ASP.NET donde nosotros definimos el StatusCode.
            // — Usamos esta en vez de OkObjectResult porque no existe "CreatedObjectResult" directamente.
            // — StatusCode = 201: lo asignamos manualmente.
            return new ObjectResult(
                ApiResponse<T>.Success(result.Value!, successMessage ?? "Recurso creado exitosamente"))
            {
                StatusCode = StatusCodes.Status201Created
            };
        }

        return MapErrorToActionResult<T>(result.Error);
    }

    // — Versión para Result SIN valor (sin tipo genérico).
    // — Se usa cuando el Handler retorna Result en vez de Result<T>
    // — (operaciones que no devuelven datos, solo éxito o error).
    public static IActionResult ToActionResult(
        this Result result,
        string successMessage = "Operación completada exitosamente")
    {
        if (result.IsSuccess)
        {
            // — new { } = objeto vacío. Como no hay datos que retornar, mandamos un objeto JSON vacío.
            return new OkObjectResult(
                ApiResponse<object>.Success(new { }, successMessage));
        }

        return MapErrorToActionResult<object>(result.Error);
    }

    // — MÉTODO PRIVADO: aquí es donde ocurre la conversión Error.Code → HTTP Status.
    // — private = solo se puede llamar desde dentro de esta clase (los métodos de arriba lo llaman).
    // —
    // — ¿CÓMO FUNCIONA?
    // — error.Code es un string (ej: "NOT_FOUND", "VALIDATION") que viene de ErrorCodes.cs.
    // — Usamos switch para comparar ese string y retornar la respuesta HTTP correcta.
    private static IActionResult MapErrorToActionResult<T>(Error error)
    {
        // — error.Code switch { ... }: es un "switch expression" de C#.
        // — Compara error.Code contra cada caso y retorna el resultado del caso que coincida.
        // — Es equivalente a varios if/else if pero más limpio.
        return error.Code switch
        {
            // — Si error.Code == "NOT_FOUND" → retorna HTTP 404 Not Found
            // — NotFoundObjectResult: clase de ASP.NET que genera respuesta 404.
            // — ApiResponse<T>.Failure(error.Message): envuelve el mensaje de error en nuestro formato JSON.
            // — Resultado: { "exitoso": false, "mensaje": "No se encontró el estudiante", ... }
            ErrorCodes.NotFound => new NotFoundObjectResult(
                ApiResponse<T>.Failure(error.Message)),

            // — Si error.Code == "VALIDATION" → retorna HTTP 400 Bad Request
            // — Esto es lo que llega cuando ValidationBehavior detecta errores.
            // — error.Message contiene: "El nombre es obligatorio; El apellido es obligatorio; ..."
            ErrorCodes.Validation => new BadRequestObjectResult(
                ApiResponse<T>.Failure(error.Message)),

            // — Si error.Code == "CONFLICT" → retorna HTTP 409 Conflict
            // — Ej: intentar crear un estudiante con un código que ya existe en la BD.
            ErrorCodes.Conflict => new ConflictObjectResult(
                ApiResponse<T>.Failure(error.Message)),

            // — Si error.Code == "INTERNAL_ERROR" → retorna HTTP 500
            // — ObjectResult + StatusCode manual porque no existe "InternalServerErrorObjectResult".
            ErrorCodes.InternalError => new ObjectResult(
                ApiResponse<T>.Failure(error.Message))
            { StatusCode = StatusCodes.Status500InternalServerError },

            // — _ = "default" o "cualquier otro caso". Si llega un código que no conocemos → 500.
            // — Es una red de seguridad: si alguien agrega un ErrorCode nuevo y no lo mapea aquí,
            // — al menos retorna 500 en vez de romper la aplicación.
            _ => new ObjectResult(
                ApiResponse<T>.Failure($"Error interno: {error.Message}"))
            { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}

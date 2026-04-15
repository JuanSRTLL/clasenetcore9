using System.Text.Json;               // — JsonSerializerOptions, JsonNamingPolicy (para serializar la respuesta a JSON)
using BaseAPI.API.Models;              // — ApiResponse<T> (formato estándar de nuestras respuestas JSON)
using BaseAPI.Application.Common.Exceptions; // — IDatabaseConnectionException, IProcedureException (interfaces de error)

namespace BaseAPI.API.Middleware;


// — Este middleware envuelve TODO en un try/catch: si algo explota en cualquier parte
// — (Controller, Handler, Repository, Procedure), la excepción llega aquí.
// —
// — ¿EN QUÉ SE DIFERENCIA DE ValidateModelAttribute Y ValidationBehavior?
// — ValidateModelAttribute: valida formato del JSON (tipos correctos) → HTTP 400
// — ValidationBehavior: valida reglas de negocio (nombre no vacío) → Result.Failure → HTTP 400
// — ExceptionHandlingMiddleware: atrapa EXCEPCIONES no controladas (BD caída, errores inesperados)
// — Es la ÚLTIMA red de seguridad. Si nada más atrapó el error, este middleware lo atrapa.
// —
// — Se registra en Program.cs: app.UseMiddleware<ExceptionHandlingMiddleware>();
public class ExceptionHandlingMiddleware
{
    // — RequestDelegate _next: función que representa el SIGUIENTE middleware en la cadena.
    // — Es el mismo concepto que "next" en ValidationBehavior:
    // —   Si llamas _next(context) → la petición sigue al siguiente middleware/Controller.
    // —   Si NO llamas _next(context) → la petición se detiene aquí.
    // — "_next" es el nombre que nosotros le damos (convención con guion bajo para campo privado).
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    // — Constructor: ASP.NET inyecta automáticamente next (el siguiente middleware) y logger.
    // — Los guardamos en _next y _logger para usarlos en InvokeAsync.
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // — InvokeAsync: ASP.NET llama este método automáticamente para CADA petición HTTP que llega.
    // — HttpContext context: contiene TODA la info de la petición (URL, headers, body)
    // — y también la respuesta (status code, body de respuesta).
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // — Llamamos al siguiente middleware → eventualmente llega al Controller → Handler → Repository.
            // — Si todo sale bien, la respuesta se envía normalmente y este catch nunca se ejecuta.
            await _next(context);
        }
        catch (Exception ex)
        {
            // — Si CUALQUIER parte del pipeline lanza una excepción que nadie atrapó,
            // — llega aquí. Ejemplos: Oracle se cayó, un null inesperado, un procedure falló.
            // —
            // — LogError: registra la excepción COMPLETA en el log (incluido stack trace).
            // — El stack trace NUNCA se envía al usuario → solo queda en el log para debugging.
            _logger.LogError(ex, "Excepción no controlada: {Message}", ex.Message);

            // — Convertimos la excepción en una respuesta HTTP con formato JSON.
            await HandleExceptionAsync(context, ex);
        }
    }

    // — Método privado: convierte una excepción en respuesta HTTP.
    // — static: no necesita acceder a _next ni _logger, solo trabaja con los parámetros.
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // — Le decimos al navegador/cliente que la respuesta será JSON (no HTML, no texto plano).
        context.Response.ContentType = "application/json";

        // — switch expression con PATTERN MATCHING: verifica el TIPO de la excepción.
        // — Según qué tipo de error sea, asigna un statusCode y un message diferentes.
        // —
        // — var (statusCode, message) = ...: es una "tupla" — un par de valores en una sola variable.
        // — Es como hacer: var statusCode = ...; var message = ...; pero en una sola línea.
        var (statusCode, message) = exception switch
        {
            // — Si la excepción implementa IDatabaseConnectionException:
            // — Significa que Oracle no responde, timeout, conexión rechazada, etc.
            // — HTTP 503 = Service Unavailable (la base de datos no está disponible).
            // — Mensaje GENÉRICO: no revelamos detalles técnicos (ej: "ORA-12541: TNS:no listener").
            IDatabaseConnectionException => (StatusCodes.Status503ServiceUnavailable,
                "Error de conexión con la base de datos. Intente más tarde."),

            // — Si la excepción implementa IProcedureException:
            // — Un stored procedure falló (ej: PRO_CREAR_ESTUDIANTE lanzó una excepción).
            // — HTTP 500 = Internal Server Error.
            // — procEx: pattern matching le da ese nombre a la excepción YA casteada a IProcedureException.
            // — procEx.ProcedureName: propiedad de la interfaz que dice cuál procedure falló.
            // — Incluimos el nombre del procedure para facilitar el debugging.
            IProcedureException procEx => (StatusCodes.Status500InternalServerError,
                $"Error en procedimiento: {procEx.ProcedureName}"),

            // — Si es ArgumentException: argumento inválido (ej: un null donde no se esperaba).
            // — HTTP 400 = Bad Request.
            // — argEx.Message: el mensaje que describe qué argumento fue inválido.
            ArgumentException argEx => (StatusCodes.Status400BadRequest,
                argEx.Message),

            // — _ = default (cualquier otra excepción que no sea ninguna de las anteriores).
            // — HTTP 500 = Internal Server Error.
            // — Mensaje GENÉRICO: NUNCA revelamos exception.Message al usuario
            // — porque podría contener info sensible (queries SQL, rutas internas, etc.).
            // — El error real ya quedó registrado en el log (LogError de arriba).
            _ => (StatusCodes.Status500InternalServerError,
                "Ocurrió un error interno. Contacte al administrador.")
        };

        // — Asignamos el código HTTP a la respuesta (200, 400, 500, 503, etc.).
        context.Response.StatusCode = statusCode;

        // — Envolvemos el mensaje en ApiResponse para mantener el mismo formato JSON
        // — que usan los demás endpoints ({ exitoso: false, mensaje: "...", datos: null, ... }).
        var response = ApiResponse<object>.Failure(message);

        // — JsonNamingPolicy.CamelCase: convierte las propiedades de C# (PascalCase)
        // — a camelCase en el JSON: Exitoso → exitoso, Mensaje → mensaje, Datos → datos.
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // — WriteAsJsonAsync: serializa "response" a JSON y lo escribe directamente
        // — en el body de la respuesta HTTP. El cliente recibe el JSON completo.
        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }
}
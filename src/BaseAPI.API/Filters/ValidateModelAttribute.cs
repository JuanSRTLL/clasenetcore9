using Microsoft.AspNetCore.Mvc;         // — BadRequestObjectResult (para generar HTTP 400)
using Microsoft.AspNetCore.Mvc.Filters; // — ActionFilterAttribute, ActionExecutingContext (sistema de filtros de ASP.NET)
using BaseAPI.Application.Common;       // — Por si se necesita ErrorCodes en futuro
using BaseAPI.API.Models;               // — ApiResponse<T> (formato JSON estándar de nuestras respuestas)

namespace BaseAPI.API.Filters;

// — ¿QUÉ ES ESTO?
// — Un FILTRO que ASP.NET ejecuta ANTES de que el código del Controller se ejecute.
// — Se usa como atributo: [ValidateModel] encima de un Controller o método.
// —
// — ¿QUÉ VALIDA?
// — El ModelState = el resultado de ASP.NET intentando convertir el JSON del body
// — a la clase C# del parámetro (ej: CrearEstudianteRequestDto).
// — Si el JSON tiene un campo con tipo incorrecto (ej: "anioMatricula": "abc" cuando espera int),
// — ASP.NET marca el ModelState como inválido ANTES de que llegue a nuestro código.
// —
// — ¿EN QUÉ SE DIFERENCIA DE ValidationBehavior?
// — ValidationBehavior valida REGLAS DE NEGOCIO (nombre no vacío, año entre 2000-2100) con FluentValidation.
// — ValidateModelAttribute valida FORMATO/TIPOS del JSON (¿es un int? ¿se pudo deserializar?).
// — Primero pasa por este filtro, después por ValidationBehavior.
// —
// — ActionFilterAttribute: clase base de ASP.NET que nos permite interceptar el ciclo de vida de un request.
// — Al heredar de ella y sobreescribir OnActionExecuting, nuestro código corre ANTES del Controller.
public class ValidateModelAttribute : ActionFilterAttribute
{
    // — OnActionExecuting: ASP.NET llama este método automáticamente ANTES de ejecutar la acción del Controller.
    // — override: estamos sobreescribiendo el método vacío que viene de ActionFilterAttribute.
    // —
    // — ActionExecutingContext context: objeto que ASP.NET nos pasa con toda la información del request:
    // —   context.ModelState → el resultado de deserializar el JSON del body
    // —   context.Result     → si le asignamos algo, ASP.NET retorna eso y NO ejecuta el Controller
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // — context.ModelState.IsValid: propiedad que ASP.NET llena automáticamente.
        // — true  = el JSON del body se convirtió correctamente a la clase C# del parámetro
        // — false = hubo problemas (tipo incorrecto, campo requerido faltante, JSON mal formado)
        // —
        // — Ejemplo: el endpoint espera CrearEstudianteRequestDto { int AnioMatricula }
        // —   Body: { "anioMatricula": 2024 }  → IsValid = true  (int válido)
        // —   Body: { "anioMatricula": "abc" }  → IsValid = false (string no es int)
        // —   Body: null o vacío                → IsValid = false
        if (!context.ModelState.IsValid)
        {
            // — ModelState es un diccionario: clave = nombre del campo, valor = lista de errores.
            // — Ej: { "AnioMatricula": ["The value 'abc' is not valid for AnioMatricula"] }
            var errors = context.ModelState
                // — Where: filtra solo los campos que SÍ tienen errores (ignora los que pasaron bien)
                .Where(x => x.Value?.Errors.Count > 0)
                // — SelectMany: "aplana" → de cada campo saca sus errores y los junta en una sola lista
                .SelectMany(x => x.Value!.Errors)
                // — Select: de cada error, extrae solo el texto del mensaje (string)
                .Select(x => x.ErrorMessage)
                // — ToList: convierte a List<string> para pasarlo a ApiResponse
                .ToList();

            // — Creamos la respuesta de error en nuestro formato estándar ApiResponse
            // — "Errores de validación" = mensaje principal
            // — errors = lista detallada ["The value 'abc' is not valid for AnioMatricula"]
            var response = ApiResponse<object>.Failure("Errores de validación", errors);

            // — BadRequestObjectResult = genera HTTP 400 Bad Request con el JSON de response.
            // —
            // — context.Result = ...: ESTA ES LA LÍNEA CLAVE.
            // — Al asignar un valor a context.Result, le decimos a ASP.NET:
            // — "Ya tengo la respuesta, NO ejecutes el método del Controller."
            // — Sin esta línea, el Controller se ejecutaría aunque el ModelState sea inválido.
            // — Es el mismo concepto de "next()" en ValidationBehavior: aquí NO llamamos al Controller.
            context.Result = new BadRequestObjectResult(response);
        }
        // — Si ModelState.IsValid es true, este método no hace nada y el Controller se ejecuta normalmente.
    }
}
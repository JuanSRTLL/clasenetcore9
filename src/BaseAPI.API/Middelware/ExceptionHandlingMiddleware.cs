using System.Text.Json;
using BaseAPI.API.Models;
using BaseAPI.Application.Common.Exceptions;

namespace BaseAPI.API.Middelware
{
    // Middelware que captura TODAS las excepciones no controladas. Y que posteriormente lo registraremos en Program.cs
    public class ExceptionHandingMiddelware
    {

        // _next: Funcion que llama  al siguiente Middelware  en el pipeline
        private readonly RequestDelegate _next;

        // Logger para registrar las excepciones  en los LOGS o en la consola NO van ir a la respuesta de la API
        private readonly ILogger<ExceptionHandingMiddelware> _logger;

        public ExceptionHandingMiddelware(RequestDelegate next, ILogger<ExceptionHandingMiddelware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                //Llamamos al siguiente middelware
                //Si todo sale bien, la respuesta se envia normalmente.
                await _next(context);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exepcion no controlada: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        //Metodo privado que convierte una excepcion en una respuesta HTTP
        public static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {

            //Definimos esto para que la respuesta sea de tipo JSON
            context.Response.ContentType = "application/json";
            var (statusCode, message) = exception switch
            {
                IDatabaseConnectionException => (StatusCodes.Status503ServiceUnavailable,
              "Error de conexion con la base de datos. Intente más tarde"),

                IProcedureException procEx => (StatusCodes.Status500InternalServerError,
                $"Error en procedimiento: {procEx.ProcedureName}"),

                ArgumentException argEx => (StatusCodes.Status400BadRequest,
                argEx.Message),

              _=> (StatusCodes.Status500InternalServerError,
              "Ocurrio un error interno. Contacte  al administrador")

            };

            context.Response.StatusCode = statusCode;

            var response = ApiResponse<Object>.Failure(message);

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            await context.Response.WriteAsJsonAsync(response, jsonOptions);
        }

    }
}

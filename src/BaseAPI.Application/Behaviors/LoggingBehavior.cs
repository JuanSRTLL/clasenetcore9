using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BaseAPI.Application.Behaviors;

/// <summary>
/// Behavior que registra automáticamente el tiempo de ejecución de cada Handler.
/// Útil para identificar operaciones lentas y monitorear rendimiento.
/// 
/// Información registrada en cada request:
/// - Nombre del Command/Query que se ejecuta
/// - Tiempo de ejecución en milisegundos
/// - Si ocurrió un error (se loggea como Error)
/// </summary>
// — TRequest: el tipo del Command o Query (ej: ListarEstudiantesQuery, CrearEstudianteCommand)
// — TResponse: el tipo de respuesta (ej: Result<List<EstudianteDto>>, Result<string>)
// — IPipelineBehavior: interfaz de MediatR que dice "yo soy un paso del pipeline"
// — Este Behavior se ejecuta para TODOS los requests, no solo los que tienen validador
// — (a diferencia de ValidationBehavior que solo actúa si hay validadores)
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // — _logger: servicio de logging de .NET que escribe en consola/archivos
    // — ILogger<LoggingBehavior<...>>: el tipo genérico le dice al logger
    // —   de qué clase viene el mensaje, así en la consola aparece:
    // —   [INF] LoggingBehavior<ListarEstudiantesQuery, Result<...>> → Iniciando...
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    // — Constructor: DI inyecta el logger automáticamente
    // — No necesitamos registrarlo manualmente — .NET lo registra con builder.Logging
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    // — Handle: el método que MediatR llama cuando este Behavior le toca en el pipeline
    // — request: el Command o Query que llegó (ej: ListarEstudiantesQuery)
    // — next: delegado que llama al SIGUIENTE paso del pipeline
    // —   Si este es el último Behavior → next() llama al Handler
    // —   Si hay más Behaviors después → next() llama al siguiente Behavior
    // — cancellationToken: permite cancelar la operación si el cliente cierra la conexión
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // — typeof(TRequest).Name: obtiene el nombre de la clase del request
        // — Ejemplo: "ListarEstudiantesQuery", "CrearEstudianteCommand"
        // — Lo guardamos en variable porque lo usamos 3 veces (inicio, fin, error)
        var requestName = typeof(TRequest).Name;

        // — Log de INICIO: registra que el request empezó a procesarse
        // — LogInformation: nivel INFO (operación normal, no es error ni advertencia)
        // — {RequestName}: placeholder que .NET reemplaza por el valor de requestName
        // —   (NO usar string interpolation $"..." con loggers — los placeholders permiten
        // —    que herramientas como Seq o Application Insights agrupen logs por patrón)
        _logger.LogInformation("Iniciando {RequestName}", requestName);

        // — Stopwatch: cronómetro de alta precisión de .NET
        // — StartNew(): crea el cronómetro Y lo inicia inmediatamente
        // — Esto mide cuántos milisegundos tarda el Handler en responder
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // — next(): llama al siguiente paso del pipeline (generalmente el Handler)
            // — await: espera a que termine de ejecutarse
            // — response: el resultado que devuelve el Handler (ej: Result<List<EstudianteDto>>)
            var response = await next();

            // — Detener el cronómetro después de que el Handler terminó exitosamente
            stopwatch.Stop();

            // — Log de FIN EXITOSO: registra que terminó y cuánto tardó
            // — ElapsedMilliseconds: los milisegundos que pasaron desde StartNew()
            // — Ejemplo en consola: "Completado ListarEstudiantesQuery en 45ms"
            // — Si ves que un query tarda 5000ms, sabes que hay un problema de rendimiento
            _logger.LogInformation(
                "Completado {RequestName} en {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            // — Retorna la respuesta del Handler tal cual, sin modificarla
            // — Este Behavior NUNCA altera la respuesta — solo observa y registra
            return response;
        }
        catch (Exception ex)
        {
            // — Si el Handler (o cualquier paso posterior) lanza una excepción,
            // — la atrapamos aquí SOLO para registrarla en el log
            stopwatch.Stop();

            // — LogError: nivel ERROR (algo falló, requiere atención)
            // — El primer parámetro (ex) es la excepción completa con su stack trace
            // — Ejemplo en consola:
            // —   [ERR] Error en CrearEstudianteCommand después de 120ms
            // —   System.InvalidOperationException: No service for type...
            // —     at BaseAPI.Infrastructure.Persistence...
            _logger.LogError(ex,
                "Error en {RequestName} después de {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            // — throw (sin "throw ex"): relanza la MISMA excepción sin perder el stack trace
            // — Si usaras "throw ex", el stack trace se reiniciaría desde esta línea
            // — y perderías la información de DÓNDE ocurrió realmente el error
            // — La excepción sigue subiendo → la atrapa ExceptionHandlingMiddleware (Clase 3)
            throw;
        }
    }
}

using FluentValidation;
using MediatR;
using BaseAPI.Application.Common; // !! Aquí viven Result<T>, Error, ErrorCodes
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Behaviors;

/// <summary>
/// Behavior del pipeline de MediatR que ejecuta validaciones AUTOMÁTICAMENTE
/// antes de que el Handler procese el request.
/// Ver PIPELINE_MEDIATR_VALIDATION.md para el diagrama completo del flujo.
/// </summary>

// — ValidationBehavior<TRequest, TResponse>
// — Es GENÉRICO (<TRequest, TResponse>) para que la misma clase sirva para TODOS los Commands y Queries.
// — MediatR reemplaza TRequest y TResponse con los tipos reales automáticamente:
// —   Ej: ValidationBehavior<CrearEstudianteCommand, Result<CrearEstudianteResponseDto>>

// — : IPipelineBehavior<TRequest, TResponse>
// — Implementamos esta interfaz de MediatR para que nos reconozca como "un paso del pipeline".
// — MediatR sabe de nosotros porque en DependencyInjection.cs lo registramos:
// —   cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

// — where TRequest : IRequest<TResponse>
// — Es una RESTRICCIÓN: solo acepta tipos que implementen IRequest<TResponse>.
// — ¿Por qué? Porque nuestros Commands y Queries implementan IRequest<Result<T>>.
// — Esto garantiza que este Behavior solo procese requests de MediatR válidos,
// — no cualquier clase random.
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // — IEnumerable<IValidator<TRequest>> = una colección de validadores para TRequest
    // — IEnumerable es como List pero de solo lectura: solo sirve para recorrer.
    // — IValidator<TRequest> = interfaz de FluentValidation para validar un tipo específico.
    // —
    // — ¿DE DÓNDE SALE ESTA LISTA?
    // — .NET la inyecta automáticamente en el constructor (más abajo).
    // — Los validators se registraron en DependencyInjection.cs con:
    // —   services.AddValidatorsFromAssembly(assembly);
    // — Esa línea escanea el proyecto, encuentra clases como CrearEstudianteCommandValidator
    // — (que hereda de AbstractValidator<CrearEstudianteCommand>), y las registra.
    // —
    // — Ejemplo: si llega un CrearEstudianteCommand → _validators = [CrearEstudianteCommandValidator]
    // —          si llega un ListarEstudiantesQuery  → _validators = [] (vacía, no tiene validator)
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    // — Constructor: .NET inyecta automáticamente los parámetros.
    // — validators viene de AddValidatorsFromAssembly, logger viene del sistema de logging.
    // —
    // — _validators = validators;
    // — "_validators" es simplemente el NOMBRE que nosotros le damos al campo privado (línea de arriba).
    // — El guion bajo (_) es una convención en C# para distinguir campos de clase vs parámetros locales.
    // — Lo que hacemos aquí es: guardar lo que .NET nos inyectó (validators) en el campo (_validators)
    // — para poder usarlo después en Handle().
    // — Sin esta línea, los validators se perderían al salir del constructor.
    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;  // — guardamos la lista inyectada en el campo de la clase
        _logger = logger;          // — igual con el logger
    }

    // — Handle(): MediatR llama este método ANTES del Handler.
    // —
    // — request: el Command/Query que envió el Controller con _mediator.Send(command).
    // —          Es el MISMO objeto, con los mismos datos.
    // —
    // — next: función que MediatR pasa. Si llamas next() → el Handler se ejecuta.
    // —        Si NO llamas next() → el Handler NUNCA se ejecuta. Así "bloqueamos" si hay errores.
    // —
    // — cancellationToken: para cancelar si el cliente se desconecta.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ═══════════════════════════════════════════════════════════════
        // PASO 1: ¿Hay validators para este request?
        // ═══════════════════════════════════════════════════════════════
        // — .Any() = "¿hay al menos uno en la lista?"
        // — Si _validators está vacía (ej: ListarEstudiantesQuery no tiene validator)
        // — → llamamos next() directo → el Handler se ejecuta sin validar nada.
        if (!_validators.Any())
        {
            return await next();
        }

        // ═══════════════════════════════════════════════════════════════
        // PASO 2: Preparar el contexto de validación
        // ═══════════════════════════════════════════════════════════════
        // — ValidationContext envuelve el request para que FluentValidation
        // — pueda acceder a sus propiedades (Nombre, Apellido, etc.) y revisarlas.
        var context = new ValidationContext<TRequest>(request);

        // ═══════════════════════════════════════════════════════════════
        // PASO 3: Ejecutar TODOS los validators
        // ═══════════════════════════════════════════════════════════════
        // — _validators.Select(v => v.ValidateAsync(...)): por cada validator, ejecuta sus reglas
        // — Task.WhenAll: los ejecuta todos en paralelo (no espera uno para empezar otro)
        // — Cada uno retorna un ValidationResult con .Errors (lista de errores, o vacía si pasó)
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // ═══════════════════════════════════════════════════════════════
        // PASO 4: Recopilar todos los errores
        // ═══════════════════════════════════════════════════════════════
        // — Where(r => r.Errors.Any()): solo toma los results que SÍ tengan errores
        // — SelectMany(r => r.Errors): junta todos los errores en una sola lista plana
        // — .ToList(): convierte a List para poder usar .Count después
        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        // ═══════════════════════════════════════════════════════════════
        // PASO 5: Si no hay errores → continuar al Handler
        // ═══════════════════════════════════════════════════════════════
        if (failures.Count == 0)
        {
            return await next();
        }

        // ═══════════════════════════════════════════════════════════════
        // PASO 6: HAY ERRORES → Retornar Failure SIN ejecutar el Handler
        // ═══════════════════════════════════════════════════════════════
        // — Como NO llamamos next(), el Handler NUNCA se ejecuta.

        // — failures.Select(f => f.ErrorMessage): de cada error, extrae solo el mensaje (string)
        // — string.Join("; ", ...): une todos los mensajes en UN solo string separados por ";"
        // — Ej: "El nombre es obligatorio; El apellido es obligatorio; Año inválido"
        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // — Registramos en el log qué request falló y con qué errores (para debugging)
        _logger.LogWarning("Validación fallida para {RequestType}: {Errors}",
            typeof(TRequest).Name, errorMessage);

        // — typeof(TResponse) nos dice qué tipo de Result espera este request
        var responseType = typeof(TResponse);

        // — CASO 1: Result<T> (ej: Result<CrearEstudianteResponseDto>)
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            // — Creamos el objeto Error con código "VALIDATION" y el mensaje combinado
            var error = new Error(ErrorCodes.Validation, errorMessage);

            // — Usamos Reflection porque no podemos escribir Result<T>.Failure(error) directamente:
            // — T es genérico y no sabemos en compilación si es CrearEstudianteResponseDto u otro tipo.
            // — GetMethod("Failure"): le pregunta a Result<T> "¿tienes un método Failure?"
            // — Invoke(null, ...): lo ejecuta (null porque es static). Equivale a Result<T>.Failure(error)
            var failureMethod = responseType.GetMethod("Failure", new[] { typeof(Error) });
            return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
        }

        // — CASO 2: Result sin tipo genérico (sin valor de retorno)
        if (responseType == typeof(Result))
        {
            var result = Result.Failure(Error.Validation(errorMessage));
            return (TResponse)(object)result;
        }

        // — Fallback: tipo desconocido → dejar pasar al Handler (no debería ocurrir)
        return await next();
    }
}

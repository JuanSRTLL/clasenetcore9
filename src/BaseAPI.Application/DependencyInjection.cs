using Microsoft.Extensions.DependencyInjection; // — IServiceCollection: el contenedor donde .NET guarda todos los servicios registrados
using FluentValidation;   // — Para AddValidatorsFromAssembly (registrar los Validators)
using System.Reflection;  // — Para Assembly.GetExecutingAssembly() (obtener referencia a este proyecto)
using BaseAPI.Application.Behaviors; // — Para referenciar ValidationBehavior
using MediatR; // — Para AddMediatR, RegisterServicesFromAssembly, AddOpenBehavior

namespace BaseAPI.Application;

// — public static class: es estática porque no necesitamos crear instancias de ella.
// — Solo contiene un método de extensión (this IServiceCollection) que se llama desde Program.cs.
// —
// — ¿POR QUÉ EXISTE ESTA CLASE?
// — En vez de registrar TODO en Program.cs (que se haría enorme),
// — cada capa (Application, Infrastructure) tiene su propio DependencyInjection.cs
// — que registra SUS servicios. Program.cs solo hace: builder.Services.AddApplication();
public static class DependencyInjection
{
    // — this IServiceCollection services: el "this" lo convierte en un EXTENSION METHOD.
    // — Eso permite llamarlo como: builder.Services.AddApplication()
    // — en vez de: DependencyInjection.AddApplication(builder.Services)
    // — Es solo azúcar sintáctica, hace lo mismo pero se lee más limpio.
    // —
    // — IServiceCollection services: es el contenedor de .NET donde se registran todos los servicios.
    // — Cada vez que hacemos services.AddX(), estamos diciendo: ".NET, cuando alguien pida X, dale esto."
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // — Assembly.GetExecutingAssembly() obtiene una referencia al proyecto ACTUAL (BaseAPI.Application).
        // — ¿Para qué? Para que MediatR y FluentValidation puedan ESCANEAR todos los archivos de este proyecto
        // — y encontrar automáticamente los Handlers, Validators, etc. sin registrarlos uno por uno.
        // — Lo guardamos en "assembly" para reutilizarlo abajo (no repetir la llamada).
        var assembly = Assembly.GetExecutingAssembly();

        // — services.AddMediatR(): registra MediatR en el contenedor de .NET.
        // — cfg => { ... }: es una función de configuración donde le decimos QUÉ registrar.
        services.AddMediatR(cfg =>
        {
            // — RegisterServicesFromAssembly(assembly): escanea BaseAPI.Application y busca
            // — clases que implementen IRequestHandler<TRequest, TResponse>.
            // — Encuentra: CrearEstudianteHandler, ListarEstudiantesHandler, etc.
            // — Los registra para que cuando hagamos _mediator.Send(command),
            // — MediatR sepa a qué Handler llamar.
            cfg.RegisterServicesFromAssembly(assembly);

            // — AddOpenBehavior: registra un Behavior en el pipeline de MediatR.
            // — typeof(ValidationBehavior<,>): el <,> significa "genérico abierto" — sirve para TODOS los tipos.
            // — Con esto, MediatR sabe que ANTES de cada Handler debe pasar por ValidationBehavior.
            // — Sin esta línea, ValidationBehavior existiría en el código pero nadie lo ejecutaría.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // — AddValidatorsFromAssembly(assembly): escanea BaseAPI.Application y busca
        // — clases que hereden de AbstractValidator<T>.
        // — Encuentra: CrearEstudianteCommandValidator (hereda AbstractValidator<CrearEstudianteCommand>)
        // — Lo registra como IValidator<CrearEstudianteCommand> en el contenedor de .NET.
        // — Después, cuando .NET crea ValidationBehavior, le inyecta estos validators automáticamente
        // — en el parámetro IEnumerable<IValidator<TRequest>> del constructor.
        services.AddValidatorsFromAssembly(assembly);

        // — return services: retornamos el mismo IServiceCollection para permitir encadenar llamadas.
        // — Ej: builder.Services.AddApplication().AddInfrastructure();
        return services;
    }
}

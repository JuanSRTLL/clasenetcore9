using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseAPI.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Behaviors
{
    public class ValidationBehaviors<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest
    {


        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehaviors<TRequest, TResponse>> _logger;

        public ValidationBehaviors(
            IEnumerable <IValidator<TRequest>> validators,
            ILogger<ValidationBehaviors<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken
            )
        {
            //Paso 1. .Any ¿Hay almenos un validador en esta peticion?
            // Si _validators esta vacia por ejemplo ListarEstudiantesQuery que no tiene un validador
            // Entonces que llame a .next directo  y continue con el handler delegado.
            if (!_validators.Any())
            {
                return await next();
            }

            //PASO 2. Prearar el contexto de la validacion
            // ValidationContext envuelve el request  para que FluentValidation pueda acceder
            //a sus propiedades (Nombre, apellido) y que pueda revisarlas

            var context = new ValidationContext<TRequest>(request);

            //Paso 3. Ejecutar todos los validadores
            // _validatos.Select  por cada validador ejecuta las reglas.
            // Task.WhenAll: Los ejecuta todos en paralelo (no esperar que se detenga uno para empezar otro)
            // Cado uno va a retornar un ValidationResult  con .Errors(Lista de errores o vacio si paso)
            var validationResult = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));


            //PASO 4.
            // Where  solo va a tomar los results que si tengan errores 
            //SelectMany  nos ayuda a juntar todos los errores en una sola lista plana
            // .ToList() Convertir  a List para posteriormente usar el .Count

            var failures = validationResult
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            //PASO 5. Si no hay errores  entonces se continua con el handler

            if (failures.Count == 0)
            {
                return await next();
            }

            //El failures.Select de cada error va a extraer solo el mensaje de cada error (string)
            //string.Join uno todos los mensajes en un SOLO string separado por ;
            // EJEMPLO: El nombre es obligatorio; El apellido es obligatorio;.....
            var errorMessage = string.Join("; ", failures.Select(f=>f.ErrorMessage));

            _logger.LogWarning("Validacion fallida para {RequestType}: {Errors}",
                typeof(TRequest).Name, errorMessage);

            // typeof(TResponse) nos dice que tipo de Result espera cada request
            var responseType = typeof(TResponse);


            // CASO 1: Result<T> (Ejemplo:CrearEstudianteResponseDto)
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {

                // Creamos el objeto Error con codigo "VALIDATION" y el mensaje combinado
                var error = new Error(ErrorCodes.Validation, errorMessage);

                // Usamos reflection porque no podemos escribir Result<T>.Failure(error) directamente:
                // T es generico  y no sabemos al momento de compilar si es CrearEstudianteResponseDTO u otro tipo
                //GetMethod("Failure preguntar  a Result<T> ¿Tiene un metodo failure?
                // Invoke(null.....) lo ejecute (null porque es static) Equivale a Result <T>.Failure(error)
                var failureMethod = responseType.GetMethod("Failure", new[] { typeof(Error) });
                return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
            }

            // CASO 2: Result sin tipo generico  (sin valor de retorno)
            if (responseType == typeof(Result))
            {
                var result = Result.Failure(Error.Validation(errorMessage));
                return (TResponse)(object)result;
            }

            // Fallback: tipo desconocido entonces que deje pasar el handler (pero NO deberia pasar porque debemos)
            // Registrar todas las validaciones dentro de la inyeccion de dependencias.
            return await next();
        }
    }
}

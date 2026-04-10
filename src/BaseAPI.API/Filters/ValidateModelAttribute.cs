using Microsoft.AspNetCore.Mvc; //Permitir usar el metodo BadRequestObjectResult para  generar los codigos HTTP 400
using Microsoft.AspNetCore.Mvc.Filters; // ActionFilterAttribute, ActionExecutingContext  (Sistemas de filtros de .NET)
using BaseAPI.API.Models;

namespace BaseAPI.API.Filters
{

    //ActionFilterAttribute: Clase base de ASP.NET  que nos permite
    //interceptar el ciclo de vida de un  request
    public class ValidateModelAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext context)
        {
         
            if (!context.ModelState.IsValid)
            {
                //ModelState es un diccionario: clave = nombre del campo, valor = lista de errores
                var errors = context.ModelState
                    // Where: Filtra solo los campos que SI tienen errores  (Ignorar los que estan bien)
                    .Where(x =>x.Value?.Errors.Count>0)
                    //Aplana  debido a que de cada campo saca los errores y los guarda en una lista
                    .SelectMany(x => x.Value!.Errors)
                    //De cada error, extrae solo el texto del mensaje (String)
                    .Select (x=>x.ErrorMessage)
                    //Convierte a List<String> para pasarlo a ApiResponse
                    .ToList();



                //Creamos la respuesta de error en nuestro formato estandar ApiResponse
                var response = ApiResponse<object>.Failure("Errores de validacion", errors);

                context.Result = new BadRequestObjectResult(response);


            }

        }
    }
}

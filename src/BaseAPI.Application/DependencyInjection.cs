using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection; //Para poder obtener referencia a este proyecto
using BaseAPI.Application.Behaviors; //Para poder referenciar ValidationBehavior
using MediatR; // para poder usar el .AddMediatR, RegisterServiceFromAssembly, AddOpenBehavior
namespace BaseAPI.Application
{
    public static class DependencyInjection
    {


        // IServiceCollection es el contenedor  de .NET que define que entregar cuando se vaya a solicitar el AddApplication

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {


            // Obtiene una referencia al proyecto actual
            // ¿Con que fin? Para que MediatR y FluentValidation puedan escanear  todos los archivos de este proyecto.
            var assembly = Assembly.GetExecutingAssembly();



            // cfg es una funcion de configuracion donde declara que registrar
            // AddMediatR obtiene un registro de MediatR en el contendor de .NET
            services.AddMediatR(cfg =>
            {

                //Para que MediatR sepa a que Handler llamar
                cfg.RegisterServicesFromAssembly(assembly);

                // Sin esta linea, ValidationBehavior  existiria en el codigo pero nadie lo llamaria
                cfg.AddOpenBehavior(typeof(ValidationBehaviors<,>));


            });
            return services;

        }
    }
}


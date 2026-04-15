// Importamos para registrar servicios
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Contatos de applicacion
using BaseAPI.Application.Contracts.Persistence;
using BaseAPI.Application.Features.Estadisticas._Shared.Contracts;
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
using BaseAPI.Infrastructure.Features.Estadisticas;
using BaseAPI.Infrastructure.Features.Estudiantes;
using BaseAPI.Infrastructure.Features.Procedures;
using BaseAPI.Infrastructure.Persistence;
// MOTOR ORACLE 
using BaseAPI.Infrastructure.Services.Oracle.Core;
// Importamos para usar IConfiguration que pasaria el appsettings a los archivos que lo requieran
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseAPI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfraestructureServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            // !! IDbConnectionFactory de (clase 1)
            services.AddScoped<IDbConnectionFactory, OracleConnectionFactory>();

            services.AddScoped<IOracleSessionHolder, OracleSessionHolder>();

            //GetRequiredService lo usamos porque ambos obtienen la misma instancia  -> COMPARTEN la misma sesion
            services.AddScoped<IOracleSession>(sp =>
            sp.GetRequiredService<IOracleSessionHolder>());

            services.AddScoped<IUnitOfWorkFactory, OracleUnitOfWorkFactory>();

            services.AddScoped<ITransactionService, OracleTransactionService>();

            //AddSingleton porque el mapper no tiene estado, solo logica  de conversacion.
            services.AddSingleton<OracleMapper>();

            services.AddScoped<IOracleExecutor, OracleExecutor>();

            services.AddScoped<ListarEstudiantesProcedure>();
            services.AddScoped<CrearEstudianteProcedure>();


            services.AddScoped<IEstudianteRepository, EstudiantesRepository>();
            services.AddScoped<IEstadisticasRepository, EstadisticasRepository>();

            return services;
        }
    }
}

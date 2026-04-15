// — Importamos IServiceCollection para registrar servicios en DI
using Microsoft.Extensions.DependencyInjection;
// — Importamos IConfiguration para pasarla a las clases que leen appsettings.json
using Microsoft.Extensions.Configuration;
// — Importamos las interfaces de persistencia definidas en Clase 1
// !! IDbConnectionFactory, IOracleSession, IOracleSessionHolder, IUnitOfWorkFactory, ITransactionService
using BaseAPI.Application.Contracts.Persistence;
// — Importamos las implementaciones que creamos en Clase 4
// !! OracleConnectionFactory, OracleSessionHolder, OracleUnitOfWorkFactory, OracleTransactionService
using BaseAPI.Infrastructure.Persistence;
// — Importamos las clases del motor Oracle (repo base)
// !! IOracleExecutor, OracleExecutor, OracleMapper
using BaseAPI.Infrastructure.Services.Oracle.Core;
// ----AGREGAR---- Importamos los contratos de repositorio (Application Layer, Clase 2)
// !! IEstudiantesRepository, IEstadisticasRepository — interfaces que DI necesita resolver
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;  // ----AGREGAR----
using BaseAPI.Application.Features.Estadisticas._Shared.Contracts; // ----AGREGAR----
// ----AGREGAR---- Importamos los Procedures (Infrastructure, Clase 5 secciones 1-4)
using BaseAPI.Infrastructure.Features.Estudiantes.Procedures;      // ----AGREGAR----
using BaseAPI.Infrastructure.Features.Estadisticas.Procedures;     // ----AGREGAR----
// ----AGREGAR---- Importamos las implementaciones de Repository (Infrastructure, Clase 5 secciones 5-6)
using BaseAPI.Infrastructure.Features.Estudiantes;                 // ----AGREGAR----
using BaseAPI.Infrastructure.Features.Estadisticas;                // ----AGREGAR----

// — Espacio de nombres de Infrastructure (raíz del proyecto)
namespace BaseAPI.Infrastructure;

// — Clase estática: solo contiene métodos de extensión (no se instancia)
// — Igual que AddApplication() en Clase 2, pero para Infrastructure
public static class DependencyInjection
{
    // — Extension Method: agrega AddInfrastructureServices a IServiceCollection
    // — "this IServiceCollection services": el "this" lo convierte en extension method
    // — Se puede llamar como: services.AddInfrastructureServices(configuration)
    // — IConfiguration: para que las clases puedan leer appsettings.json (connection string)
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ============================================================
        // — 1. PERSISTENCE — Conexiones y transacciones Oracle
        // ============================================================

        // — Scoped: una instancia de OracleConnectionFactory por request HTTP
        // !! IDbConnectionFactory (Clase 1 contrato) → OracleConnectionFactory (Clase 4 sección 2)
        // — ¿Por qué Scoped? Porque depende de IOracleSessionHolder que también es Scoped
        services.AddScoped<IDbConnectionFactory, OracleConnectionFactory>();

        // — Scoped: una instancia de OracleSessionHolder por request HTTP
        // !! IOracleSessionHolder (Clase 1 contrato) → OracleSessionHolder (Clase 4 sección 3)
        // — ¿Por qué Scoped? Porque cada request tiene su propia sesión transaccional
        services.AddScoped<IOracleSessionHolder, OracleSessionHolder>();

        // — IOracleSession apunta a la MISMA INSTANCIA que IOracleSessionHolder
        // — Truco de DI: cuando alguien pida IOracleSession, se le da el mismo OracleSessionHolder
        // — ¿Por qué? Porque IOracleSession es de SOLO LECTURA (HasActiveSession, CurrentConnection)
        // — y IOracleSessionHolder tiene los métodos de ESCRITURA (BeginScope)
        // — El OracleExecutor pide IOracleSession (solo lee), el TransactionService pide IOracleSessionHolder (lee y escribe)
        // — Ambos obtienen la MISMA instancia → comparten la sesión
        // !! IOracleSession (Clase 1 contrato) → misma instancia que IOracleSessionHolder
        services.AddScoped<IOracleSession>(sp =>
            sp.GetRequiredService<IOracleSessionHolder>());

        // — Scoped: fábrica que crea UnitOfWork con conexión nueva
        // !! IUnitOfWorkFactory (Clase 1 contrato) → OracleUnitOfWorkFactory (Clase 4 sección 4)
        // — NOTA: IUnitOfWork NO se registra directamente porque necesita una DbConnection
        // —   que solo la fábrica sabe crear. Los Handlers usan factory.CreateAsync().
        services.AddScoped<IUnitOfWorkFactory, OracleUnitOfWorkFactory>();

        // — Scoped: servicio de transacciones
        // !! ITransactionService (Clase 1 contrato) → OracleTransactionService (Clase 4 sección 5)
        services.AddScoped<ITransactionService, OracleTransactionService>();

        // ============================================================
        // — 2. ORACLE CORE — Motor de ejecución y mapeo (del repo base)
        // ============================================================

        // — Singleton: OracleMapper NO tiene estado (solo lógica de conversión)
        // — Una sola instancia para toda la aplicación (más eficiente)
        // — ¿Por qué Singleton y no Scoped? Porque MapAsync no guarda nada entre llamadas
        // — Solo recibe un reader y retorna una lista — es "puro" (sin efectos secundarios)
        services.AddSingleton<OracleMapper>();

        // — Scoped: OracleExecutor depende de IOracleSession que es Scoped
        // — ¿Por qué IOracleExecutor y no OracleExecutor directamente?
        // — Porque los archivos que lo usan (Procedures) dependen del CONTRATO (interfaz),
        // — no de la clase concreta. Así podemos crear un MockExecutor para pruebas.
        // !! IOracleExecutor (repo base) → OracleExecutor (repo base)
        services.AddScoped<IOracleExecutor, OracleExecutor>();

        // ============================================================
        // ----AGREGAR---- 3. PROCEDURES — Clases que ejecutan stored procedures de Oracle
        // ============================================================

        // — Scoped: una instancia por request HTTP (misma vida que IOracleExecutor)
        // — ¿Por qué Scoped? Porque dependen de IOracleExecutor que también es Scoped
        // — Si fueran Singleton, no podrían usar IOracleExecutor Scoped (error de captive dependency)
        // !! ListarEstudiantesProcedure (sección 1) — ejecuta PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES
        services.AddScoped<ListarEstudiantesProcedure>();           // ----AGREGAR----
        // !! CrearEstudianteProcedure (sección 2) — ejecuta PAQ_ESTUDIANTES.PRO_CREAR_ESTUDIANTE
        services.AddScoped<CrearEstudianteProcedure>();             // ----AGREGAR----
        // !! MatriculadosPorAnioProcedure (sección 3) — ejecuta PAQ_ESTADISTICAS.PRO_MATRICULADOS_POR_ANIO
        services.AddScoped<MatriculadosPorAnioProcedure>();        // ----AGREGAR----
        // !! MatriculadosPorProgramaProcedure (sección 4) — ejecuta PAQ_ESTADISTICAS.PRO_MATRICULADOS_POR_PROGRAMA
        services.AddScoped<MatriculadosPorProgramaProcedure>();    // ----AGREGAR----

        // ============================================================
        // ----AGREGAR---- 4. REPOSITORIES — Puente entre Application e Infrastructure
        // ============================================================

        // — AddScoped<INTERFAZ, IMPLEMENTACIÓN>:
        // —   Cuando alguien pida IEstudiantesRepository → DI le da EstudiantesRepository
        // — ¿Por qué interfaz → implementación?
        // —   Los Handlers (Clase 2) piden IEstudiantesRepository (el contrato)
        // —   DI les inyecta EstudiantesRepository (la implementación con Oracle)
        // —   El Handler NUNCA sabe que hay Oracle debajo — solo conoce la interfaz
        // !! IEstudiantesRepository (Clase 2) → EstudiantesRepository (sección 5)
        services.AddScoped<IEstudiantesRepository, EstudiantesRepository>();   // ----AGREGAR----
        // !! IEstadisticasRepository (Clase 2) → EstadisticasRepository (sección 6)
        services.AddScoped<IEstadisticasRepository, EstadisticasRepository>(); // ----AGREGAR----

        // — Retornamos services para permitir encadenamiento:
        // —   builder.Services.AddInfrastructureServices(config).AddOtroServicio();
        return services;
    }
}
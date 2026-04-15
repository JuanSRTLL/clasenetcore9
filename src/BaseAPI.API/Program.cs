using BaseAPI.Application;
using BaseAPI.Infrastructure;
using BaseAPI.API.Middleware;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

// ============================================================
// Program.cs — Punto de entrada de la aplicación ASP.NET Core
// ============================================================
// Aquí se configura TODO lo que necesita la API para funcionar:
// 1. Servicios (DI Container)
// 2. Middleware Pipeline (cómo se procesan las peticiones)
// 3. Swagger para pruebas
// 4. CORS para permitir requests del frontend
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICIOS — Registro en el contenedor de Dependency Injection
// ============================================================

// Configurar controladores con opciones de serialización JSON


// --- REEMPLAZAR: la línea "builder.Services.AddControllers();" se cambia por esto ---
// — AddControllers(): escanea el proyecto y registra todos los Controllers
// —   (EstudiantesController, EstadisticasController, etc.)
// — AddJsonOptions(): configura cómo C# serializa los objetos a JSON
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // — WhenWritingNull: si una propiedad es null, NO la incluye en el JSON
        // — Ejemplo: si Correo es null → no aparece "correo": null en el JSON
        // — Resultado: JSON más limpio y liviano
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
// ============================================================
// SWAGGER — Interfaz visual para probar la API
// ============================================================
// Swagger genera documentación automática de todos los endpoints
// y permite probarlos directamente desde el navegador.
// Acceso: http://localhost:5000/swagger
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Sistema Universitario",
        Version = "v1",
        Description = "API RESTful con Clean Architecture, CQRS y Oracle."
    });

    // Incluir comentarios XML de los controllers (summary, remarks, etc.)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ============================================================
// CORS — Cross-Origin Resource Sharing
// ============================================================
// Permite que un frontend en otro dominio (ej: localhost:3000)
// pueda hacer peticiones a esta API (ej: localhost:5000).
// Sin CORS, el navegador bloquea las peticiones por seguridad.
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()    // Permitir cualquier origen (para clase)
              .AllowAnyMethod()    // Permitir GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader();   // Permitir cualquier header
    });
});

// ============================================================
// CAPAS DE LA APLICACIÓN — Registro de servicios por capa
// ============================================================
// Cada capa tiene su propio método de extensión AddXxx() que registra
// sus servicios en el contenedor DI. Esto mantiene Program.cs limpio.
// ============================================================

// Application Layer: MediatR, FluentValidation, Behaviors
builder.Services.AddApplication();

// Infrastructure Layer: Oracle, Repositories
builder.Services.AddInfrastructureServices(builder.Configuration);

// ============================================================
// BUILD — Construir la aplicación
// ============================================================

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE — Orden de procesamiento de cada request
// ============================================================

// 1. EXCEPCIÓN HANDLING — Atrapar cualquier excepción no controlada
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. SWAGGER — Solo disponible en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Sistema Universitario v1");
        c.DocumentTitle = "API Sistema Universitario - Swagger";
        c.RoutePrefix = "swagger";
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
    });
}

// 3. CORS
app.UseCors("AllowAll");

// 4. CONTROLLERS — Mapear las rutas a los controllers
app.MapControllers();

// ============================================================
// RUN — Iniciar el servidor
// ============================================================
app.Run();

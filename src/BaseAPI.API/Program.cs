// — Creamos el builder: configura los servicios antes de arrancar el servidor
var builder = WebApplication.CreateBuilder(args);

// — Registramos los controllers (clases que reciben peticiones HTTP)
builder.Services.AddControllers();

// ═══════════════════════════════════════════════════════════════
// NOTA: En clases posteriores se agregarán aquí:
//   Swagger (Clase 3)                         → Para probar la API visualmente
//   CORS (Clase 3)                            → Para permitir peticiones desde un frontend
//   JSON Options (Clase 3)                    → Para configurar serialización
//   AddApplication() (Clase 2)                → MediatR y validadores
//   AddInfrastructureServices() (Clase 4)     → Oracle y persistencia
// ═══════════════════════════════════════════════════════════════

// — Construimos la aplicación con los servicios registrados
var app = builder.Build();

// ═══════════════════════════════════════════════════════════════
// NOTA: En Clase 3 se agregará aquí:
//   app.UseMiddleware<ExceptionHandlingMiddleware>();
//   app.UseSwagger() / app.UseSwaggerUI();
//   app.UseCors("AllowAll");
// ═══════════════════════════════════════════════════════════════

// — Mapear los controllers: conecta las rutas HTTP con los métodos de los controllers
app.MapControllers();

// — Arrancar el servidor y escuchar peticiones
app.Run();

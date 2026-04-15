// !! MediatR (NuGet) — IMediator para enviar Queries y Commands al pipeline
using MediatR;
// — ASP.NET Core MVC: ControllerBase, atributos de routing, IActionResult
using Microsoft.AspNetCore.Mvc;
// !! ResultExtensions.cs (sección 2) — Extension Methods .ToActionResult() y .ToCreatedResult()
using BaseAPI.API.Extensions;
// !! ValidateModelAttribute.cs (sección 3) — atributo que valida el body del POST
using BaseAPI.API.Filters;
// !! EstudiantesDTOs.cs (Clase 2, sección 1.1) — CrearEstudianteRequestDto para el body del POST
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
// !! ListarEstudiantesQuery.cs (Clase 2, sección 2.1) — Query para listar estudiantes
using BaseAPI.Application.Features.Estudiantes.Queries.ListarEstudiantes;
// !! CrearEstudianteCommand.cs (Clase 2, sección 3.1) — Command para crear estudiante
using BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante;

// — Espacio de nombres de Controllers
namespace BaseAPI.API.Controllers;

// — [ApiController]: activa binding automático de JSON, respuestas 400 automáticas
// — [Route("api/[controller]")]: la URL será api/estudiantes
// —   [controller] se reemplaza por "Estudiantes" (sin el sufijo "Controller")
// !! ValidateModelAttribute.cs (sección 3) — valida el body del POST automáticamente
[ApiController]
[Route("api/[controller]")]
[ValidateModel]
// — ControllerBase: clase base de ASP.NET para Controllers de API (sin vistas)
// — No confundir con Controller (que incluye soporte para vistas Razor)
public class EstudiantesController : ControllerBase
{
    // !! MediatR (NuGet) — interfaz para enviar mensajes al pipeline
    private readonly IMediator _mediator;

    // — Constructor: .NET inyecta IMediator automáticamente
    // — MediatR se registró en DependencyInjection.cs (Clase 2, sección 6):
    // —   services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
    public EstudiantesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/estudiantes — Lista todos los estudiantes
    // ═══════════════════════════════════════════════════════════════
    // — [HttpGet]: este método se ejecuta cuando llega un GET a /api/estudiantes
    // — Retorna IActionResult: puede ser 200 OK o 404 Not Found
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        // !! ListarEstudiantesQuery.cs (Clase 2, sección 2.1) — mensaje de lectura
        // — Creamos el Query (no tiene parámetros — lista TODOS)
        var query = new ListarEstudiantesQuery();

        // !! MediatR (NuGet) — Send() envía el Query al pipeline:
        // — Pipeline: LoggingBehavior → ValidationBehavior → ListarEstudiantesHandler
        // !! ListarEstudiantesHandler.cs (Clase 2, sección 2.2) — procesa el Query
        // !! Result.cs (Clase 1) — retorna Result<List<EstudianteDto>>
        var result = await _mediator.Send(query, cancellationToken);

        // !! ResultExtensions.cs (sección 2) — traduce Result a HTTP:
        // — result.IsSuccess → 200 OK + ApiResponse.Success(lista, "mensaje")
        // — result.IsFailure → 404 / 400 / 500 según el ErrorCode
        return result.ToActionResult("Estudiantes obtenidos exitosamente");
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/estudiantes — Crea un estudiante nuevo
    // ═══════════════════════════════════════════════════════════════
    // — [HttpPost]: este método se ejecuta cuando llega un POST a /api/estudiantes
    // — [FromBody]: ASP.NET deserializa el JSON del body en CrearEstudianteRequestDto
    [HttpPost]
    public async Task<IActionResult> Crear(
        // !! EstudiantesDTOs.cs (Clase 2, sección 1.1) — DTO que representa el body JSON
        // — [FromBody]: ASP.NET lee el body de la petición y lo convierte en este DTO
        // — Si el JSON no coincide con el DTO → ValidateModelAttribute retorna 400
        [FromBody] CrearEstudianteRequestDto request,
        CancellationToken cancellationToken)
    {
        // !! CrearEstudianteCommand.cs (Clase 2, sección 3.1) — mensaje de escritura
        // — Extraemos los datos del DTO y creamos el Command (record inmutable)
        // — ¿Por qué no pasar el DTO directamente al Handler?
        // —   1. El Command es inmutable (record) — seguridad en el pipeline
        // —   2. El Command podría agregar datos que no vienen del DTO (ej: usuario autenticado)
        // —   3. Separación clara entre "qué recibe la API" y "qué procesa Application"
        var command = new CrearEstudianteCommand(
            request.Nombre, request.Apellido, request.Identificacion,
            request.Correo, request.Programa, request.AnioMatricula);

        // !! MediatR (NuGet) — Send() envía el Command al pipeline:
        // — Pipeline: LoggingBehavior → ValidationBehavior (ejecuta CrearEstudianteCommandValidator)
        // —           → CrearEstudianteHandler → Repository → Oracle
        // !! CrearEstudianteHandler.cs (Clase 2, sección 3.3) — procesa el Command
        // !! Result.cs (Clase 1) — retorna Result<CrearEstudianteResponseDto>
        var result = await _mediator.Send(command, cancellationToken);

        // !! ResultExtensions.cs (sección 2) — ToCreatedResult para POST:
        // — result.IsSuccess → 201 Created + ApiResponse.Success(dto, "mensaje")
        // — result.IsFailure → 409 Conflict / 400 / 500 según el ErrorCode
        return result.ToCreatedResult("Estudiante creado exitosamente");
    }
}
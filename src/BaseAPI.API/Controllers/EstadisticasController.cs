// !! MediatR (NuGet) — IMediator para enviar Queries al pipeline
using MediatR;
// — ASP.NET Core MVC: ControllerBase, atributos de routing
using Microsoft.AspNetCore.Mvc;
// !! ResultExtensions.cs (sección 2) — Extension Method .ToActionResult()
using BaseAPI.API.Extensions;
// !! MatriculadosPorAnioQuery.cs (Clase 2, sección 4.3) — Query sin parámetros
using BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorAnio;
// !! MatriculadosPorProgramaQuery.cs (Clase 2, sección 4.4) — Query con parámetro Anio
using BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorPrograma;

// — Espacio de nombres de Controllers
namespace BaseAPI.API.Controllers;

// — [ApiController]: activa binding automático
// — [Route("api/[controller]")]: URL base = api/estadisticas
// — NO tiene [ValidateModel] porque no recibe body JSON (solo GET)
[ApiController]

[Route("api/[controller]")]
public class EstadisticasController : ControllerBase
{
    // !! MediatR (NuGet) — interfaz del mediador
    private readonly IMediator _mediator;

    // — Constructor: .NET inyecta IMediator
    public EstadisticasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/estadisticas/matriculados-por-anio
    // ═══════════════════════════════════════════════════════════════
    // — Retorna la cantidad de matriculados agrupados por año
    // — Para la gráfica de barras del dashboard: eje X = año, eje Y = cantidad
    // — "matriculados-por-anio" se agrega a la ruta base: api/estadisticas/matriculados-por-anio
    [HttpGet("matriculados-por-anio")]
    public async Task<IActionResult> MatriculadosPorAnio(CancellationToken cancellationToken)
    {
        // !! MatriculadosPorAnioQuery.cs (Clase 2, sección 4.3) — Query sin parámetros
        var query = new MatriculadosPorAnioQuery();

        // !! MediatR — pipeline → MatriculadosPorAnioHandler (Clase 2, sección 4.3)
        // !! Result.cs (Clase 1) — retorna Result<List<MatriculadosPorAnioDto>>
        var result = await _mediator.Send(query, cancellationToken);

        // !! ResultExtensions.cs (sección 2) — ToActionResult para GET
        return result.ToActionResult("Estadísticas de matriculados por año");
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/estadisticas/matriculados-por-programa/{anio}
    // ═══════════════════════════════════════════════════════════════
    // — Retorna la cantidad de matriculados por programa para un año específico
    // — Para la gráfica detallada: eje X = programa, eje Y = cantidad
    // — {anio:int} = parámetro de ruta con restricción de tipo (solo acepta números)
    // — Ejemplo: /api/estadisticas/matriculados-por-programa/2024
    [HttpGet("matriculados-por-programa/{anio:int}")]
    public async Task<IActionResult> MatriculadosPorPrograma(
        // — ASP.NET extrae "anio" de la URL automáticamente
        // — Si la URL es /matriculados-por-programa/2024, anio = 2024
        int anio, CancellationToken cancellationToken)
    {
        // !! MatriculadosPorProgramaQuery.cs (Clase 2, sección 4.4) — Query CON parámetro
        // — Pasamos el año que viene de la URL al Query
        var query = new MatriculadosPorProgramaQuery(anio);

        // !! MediatR — pipeline → MatriculadosPorProgramaHandler (Clase 2, sección 4.4)
        // !! Result.cs (Clase 1) — retorna Result<List<MatriculadosPorProgramaDto>>
        var result = await _mediator.Send(query, cancellationToken);

        // !! ResultExtensions.cs (sección 2) — ToActionResult para GET
        return result.ToActionResult($"Estadísticas de matriculados por programa - Año {anio}");
    }
}
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estadisticas._Shared.Contracts;
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorPrograma;

/// <summary>
/// Handler para obtener la cantidad de matriculados por programa en un año dado.
/// </summary>
public class MatriculadosPorProgramaHandler : IRequestHandler<MatriculadosPorProgramaQuery, Result<List<MatriculadosPorProgramaDto>>>
{
    private readonly IEstadisticasRepository _repository;
    private readonly ILogger<MatriculadosPorProgramaHandler> _logger;

    public MatriculadosPorProgramaHandler(
        IEstadisticasRepository repository,
        ILogger<MatriculadosPorProgramaHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<MatriculadosPorProgramaDto>>> Handle(
        MatriculadosPorProgramaQuery request,
        CancellationToken cancellationToken)
    {
        var datos = await _repository.ObtenerMatriculadosPorProgramaAsync(request.Anio, cancellationToken);

        _logger.LogInformation("Estadísticas por programa para año {Anio}: {Count} programas",
            request.Anio, datos.Count);

        return datos;
    }
}

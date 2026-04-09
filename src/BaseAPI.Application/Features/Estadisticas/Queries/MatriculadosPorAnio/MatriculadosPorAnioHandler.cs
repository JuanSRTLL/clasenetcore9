using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estadisticas._Shared.Contracts;
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorAnio;

/// <summary>
/// Handler para obtener la cantidad de matriculados por año.
/// </summary>
public class MatriculadosPorAnioHandler : IRequestHandler<MatriculadosPorAnioQuery, Result<List<MatriculadosPorAnioDto>>>
{
    private readonly IEstadisticasRepository _repository;
    private readonly ILogger<MatriculadosPorAnioHandler> _logger;

    public MatriculadosPorAnioHandler(
        IEstadisticasRepository repository,
        ILogger<MatriculadosPorAnioHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<MatriculadosPorAnioDto>>> Handle(
        MatriculadosPorAnioQuery request,
        CancellationToken cancellationToken)
    {
        var datos = await _repository.ObtenerMatriculadosPorAnioAsync(cancellationToken);

        _logger.LogInformation("Estadísticas por año consultadas: {Count} registros", datos.Count);

        return datos;
    }
}

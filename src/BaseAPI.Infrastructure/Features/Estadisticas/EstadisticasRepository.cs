using BaseAPI.Application.Features.Estadisticas._Shared.Contracts;
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;

namespace BaseAPI.Infrastructure.Features.Estadisticas;

public class EstadisticasRepository : IEstadisticasRepository
{
    public Task<List<MatriculadosPorAnioDto>> ObtenerMatriculadosPorAnioAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Pendiente de implementar");
    }

    public Task<List<MatriculadosPorProgramaDto>> ObtenerMatriculadosPorProgramaAsync(int anio, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Pendiente de implementar");
    }
}

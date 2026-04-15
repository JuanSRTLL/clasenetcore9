using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;

namespace BaseAPI.Application.Features.Estadisticas._Shared.Contracts;

/// <summary>
/// Contrato del repositorio de estadísticas.
/// Define las consultas necesarias para las gráficas del dashboard.
/// </summary>
public interface IEstadisticasRepository
{
    /// <summary>
    /// Obtiene la cantidad de estudiantes matriculados agrupados por año.
    /// Llama al procedimiento: PAQ_ESTADISTICAS.PRO_MATRICULADOS_POR_ANIO
    /// </summary>
    Task<List<MatriculadosPorAnioDto>> ObtenerMatriculadosPorAnioAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la cantidad de estudiantes matriculados agrupados por programa para un año específico.
    /// Llama al procedimiento: PAQ_ESTADISTICAS.PRO_MATRICULADOS_POR_PROGRAMA
    /// </summary>
    Task<List<MatriculadosPorProgramaDto>> ObtenerMatriculadosPorProgramaAsync(int anio, CancellationToken cancellationToken = default);
}

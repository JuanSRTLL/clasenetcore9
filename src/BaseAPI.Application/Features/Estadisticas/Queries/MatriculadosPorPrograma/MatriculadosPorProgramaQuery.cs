using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;
using MediatR;

namespace BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorPrograma;

/// <summary>
/// Query para obtener la cantidad de matriculados por programa en un año específico.
/// Se usa para la gráfica detallada (desglose por programa académico).
/// </summary>
public record MatriculadosPorProgramaQuery(
    int Anio
) : IRequest<Result<List<MatriculadosPorProgramaDto>>>;

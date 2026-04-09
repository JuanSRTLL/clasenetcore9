using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;
using MediatR;

namespace BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorPrograma;

/// <summary>
/// Query para obtener la cantidad de matriculados por programa en un año específico.
/// </summary>
public record MatriculadosPorProgramaQuery(
    int Anio
) : IRequest<Result<List<MatriculadosPorProgramaDto>>>;

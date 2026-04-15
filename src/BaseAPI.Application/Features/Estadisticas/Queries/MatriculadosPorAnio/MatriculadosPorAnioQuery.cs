using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;
using MediatR;

namespace BaseAPI.Application.Features.Estadisticas.Queries.MatriculadosPorAnio;

/// <summary>
/// Query para obtener la cantidad de matriculados agrupados por año.
/// Se usa para la gráfica general del dashboard.
/// </summary>
public record MatriculadosPorAnioQuery : IRequest<Result<List<MatriculadosPorAnioDto>>>;

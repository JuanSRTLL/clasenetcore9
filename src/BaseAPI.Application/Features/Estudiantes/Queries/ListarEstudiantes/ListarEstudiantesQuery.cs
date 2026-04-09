using System.Collections.Generic;
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;

namespace BaseAPI.Application.Features.Estudiantes.Queries.ListarEstudiantes
{
    public record ListarEstudiantesQuery : IRequest<Result<List<EstudiantesDTO>>>;
}

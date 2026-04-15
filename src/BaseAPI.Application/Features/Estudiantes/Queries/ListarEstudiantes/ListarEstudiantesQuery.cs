using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;

namespace BaseAPI.Application.Features.Estudiantes.Queries.ListarEstudiantes;

/// <summary>
/// Query para listar todos los estudiantes.
/// 
/// En CQRS, una Query representa una LECTURA de datos (no modifica estado).
/// A diferencia de un Command, las Queries son idempotentes: puedes ejecutarlas
/// múltiples veces sin efectos secundarios.
/// </summary>
public record ListarEstudiantesQuery : IRequest<Result<List<EstudianteDto>>>;

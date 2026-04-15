using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;

namespace BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante;

/// <summary>
/// Command para crear un nuevo estudiante.
/// Este es un Command porque MODIFICA estado (inserta un registro en la BD).
/// </summary>
public record CrearEstudianteCommand(
    string Nombre,
    string Apellido,
    string Identificacion,
    string? Correo,
    string Programa,
    int AnioMatricula
) : IRequest<Result<CrearEstudianteResponseDto>>;

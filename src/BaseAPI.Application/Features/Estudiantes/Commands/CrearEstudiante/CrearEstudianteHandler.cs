// !! Result.cs + ErrorCodes.cs (Application/Common) — importamos Result<T>, Error, ErrorCodes
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante;

/// <summary>
/// Handler para crear un nuevo estudiante.
/// Delega la inserción al repositorio (que a su vez llama al procedure Oracle).
/// </summary>
// !! Result.cs — El Handler retorna Result<CrearEstudianteResponseDto> (puede ser éxito o error)
public class CrearEstudianteHandler : IRequestHandler<CrearEstudianteCommand, Result<CrearEstudianteResponseDto>>
{
    private readonly IEstudiantesRepository _repository;
    private readonly ILogger<CrearEstudianteHandler> _logger;

    public CrearEstudianteHandler(
        IEstudiantesRepository repository,
        ILogger<CrearEstudianteHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // !! Result.cs — El tipo de retorno es Result<CrearEstudianteResponseDto>
    public async Task<Result<CrearEstudianteResponseDto>> Handle(
        CrearEstudianteCommand request,
        CancellationToken cancellationToken)
    {
        var resultado = await _repository.CrearEstudianteAsync(
            request.Nombre,
            request.Apellido,
            request.Identificacion,
            request.Correo,
            request.Programa,
            request.AnioMatricula,
            cancellationToken);

        if (resultado != "OK")
        {
            _logger.LogWarning("Error al crear estudiante {Identificacion}: {Resultado}",
                request.Identificacion, resultado);
            // !! Result.cs + ErrorCodes.cs — Error.Conflict() crea un Error con código ErrorCodes.Conflict
            // !! Esto se convierte implícitamente en Result<T>.Failure(error) gracias al operador implicit
            // — El stored procedure de Oracle retorna un string como:
            // —   "ERROR: El estudiante con identificación 123 ya existe"
            // — .Replace("ERROR: ", "") quita el prefijo técnico y deja solo el mensaje limpio:
            // —   "El estudiante con identificación 123 ya existe"
            // — Así el mensaje que llega al usuario final NO tiene el prefijo "ERROR: "
            return Error.Conflict(resultado.Replace("ERROR: ", ""));
        }

        _logger.LogInformation("Estudiante creado: {Nombre} {Apellido} ({Identificacion})",
            request.Nombre, request.Apellido, request.Identificacion);

        return new CrearEstudianteResponseDto
        {
            Mensaje = "Estudiante creado exitosamente"
        };
    }
}

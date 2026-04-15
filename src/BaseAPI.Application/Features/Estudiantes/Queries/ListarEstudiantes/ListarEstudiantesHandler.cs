// !! Result.cs + ErrorCodes.cs (Application/Common) — importamos Result<T>, Error, ErrorCodes
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Features.Estudiantes.Queries.ListarEstudiantes;

/// <summary>
/// Handler que procesa la query ListarEstudiantesQuery.
/// Simplemente delega al repositorio y retorna la lista de estudiantes.
/// </summary>
// !! Result.cs — El Handler retorna Result<List<EstudianteDto>> en vez de List<EstudianteDto>
// !! Así quien lo llame sabe que puede ser éxito (la lista) o error (Error.NotFound, etc.)
public class ListarEstudiantesHandler : IRequestHandler<ListarEstudiantesQuery, Result<List<EstudianteDto>>>
{
    private readonly IEstudiantesRepository _repository;
    private readonly ILogger<ListarEstudiantesHandler> _logger;

    public ListarEstudiantesHandler(
        IEstudiantesRepository repository,
        ILogger<ListarEstudiantesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // !! Result.cs — El tipo de retorno es Result<List<EstudianteDto>>
    public async Task<Result<List<EstudianteDto>>> Handle(
        ListarEstudiantesQuery request,
        CancellationToken cancellationToken)
    {
        var estudiantes = await _repository.ListarEstudiantesAsync(cancellationToken);

        _logger.LogInformation("Se listaron {Count} estudiantes", estudiantes.Count);

        // !! Result.cs — conversión implícita: "return estudiantes" se convierte automáticamente
        // !! en Result<List<EstudianteDto>>.Success(estudiantes) gracias al operador implicit
        return estudiantes;
    }
}

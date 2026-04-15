using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;

namespace BaseAPI.Application.Features.Estudiantes._Shared.Contracts;

/// <summary>
/// Contrato del repositorio de estudiantes.
/// Define las operaciones de base de datos para el módulo de Admisiones.
/// 
/// Implementado por EstudiantesRepository en la capa Infrastructure,
/// que a su vez delega a los procedures de Oracle (PAQ_ESTUDIANTES).
/// </summary>
public interface IEstudiantesRepository
{
    /// <summary>
    /// Lista todos los estudiantes registrados.
    /// Llama al procedimiento: PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES
    /// </summary>
    Task<List<EstudianteDto>> ListarEstudiantesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un nuevo estudiante.
    /// Llama al procedimiento: PAQ_ESTUDIANTES.PRO_CREAR_ESTUDIANTE
    /// </summary>
    /// <returns>"OK" si se creó correctamente, "ERROR: ..." si hubo problema</returns>
    Task<string> CrearEstudianteAsync(
        string nombre, string apellido, string identificacion,
        string? correo, string programa, int anioMatricula,
        CancellationToken cancellationToken = default);
}

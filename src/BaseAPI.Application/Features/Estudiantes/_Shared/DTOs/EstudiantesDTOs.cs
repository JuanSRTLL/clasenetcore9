namespace BaseAPI.Application.Features.Estudiantes._Shared.DTOs;

/// <summary>
/// DTOs para el feature de Estudiantes (módulo Admisiones).
/// </summary>

/// <summary>DTO con los datos de un estudiante para listar</summary>
public class EstudianteDto
{
    public int IdEstudiante { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string Programa { get; set; } = string.Empty;
    public int AnioMatricula { get; set; }
    public DateTime FechaRegistro { get; set; }
}

/// <summary>DTO para la solicitud de creación de un estudiante</summary>
public class CrearEstudianteRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string Programa { get; set; } = string.Empty;
    public int AnioMatricula { get; set; }
}

/// <summary>DTO con la respuesta después de crear un estudiante</summary>
public class CrearEstudianteResponseDto
{
    public string Mensaje { get; set; } = string.Empty;
}

namespace BaseAPI.Application.Features.Estadisticas._Shared.DTOs;

/// <summary>
/// DTOs para el feature de Estadísticas (módulo Reportes).
/// Se usan para las gráficas de matriculados por año y por programa.
/// </summary>

/// <summary>DTO con la cantidad de matriculados en un año específico</summary>
public class MatriculadosPorAnioDto
{
    public int Anio { get; set; }
    public int Cantidad { get; set; }
}

/// <summary>DTO con la cantidad de matriculados en un programa para un año dado</summary>
public class MatriculadosPorProgramaDto
{
    public string Programa { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

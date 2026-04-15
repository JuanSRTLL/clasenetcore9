namespace BaseAPI.API.Models;

// — Clase genérica: T es el tipo de datos que envuelve la respuesta
// — Ejemplo: ApiResponse<List<EstudianteDto>>, ApiResponse<CrearEstudianteResponseDto>
public class ApiResponse<T>
{
    // — ¿La operación fue exitosa? El frontend revisa esto PRIMERO
    public bool Exitoso { get; set; }
    // — Mensaje descriptivo: "Estudiantes obtenidos exitosamente" o "El nombre es obligatorio"
    public string Mensaje { get; set; } = string.Empty;
    // — Los datos de la respuesta (solo tiene valor cuando Exitoso=true)
    // — T? = puede ser null (cuando es error, Datos es null)
    public T? Datos { get; set; }
    // — Lista de errores detallados (para validación del ModelState)
    // — Ejemplo: ["El nombre es obligatorio", "El año debe ser mayor a 2000"]
    public List<string> Errores { get; set; } = new();
    // — Cuándo se generó esta respuesta (útil para debugging)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // — Fábrica de ÉXITO: crea una respuesta exitosa con datos
    // — Ejemplo: ApiResponse<List<EstudianteDto>>.Success(lista, "Obtenidos")
    public static ApiResponse<T> Success(T data, string? mensaje = null)
    {
        return new ApiResponse<T>
        {
            // — Marcamos como exitoso
            Exitoso = true,
            // — Mensaje descriptivo (con valor por defecto si no se proporciona)
            Mensaje = mensaje ?? "Operación exitosa",
            // — Los datos reales de la respuesta
            Datos = data,
            // — Momento en que se generó la respuesta
            Timestamp = DateTime.UtcNow
        };
    }

    // — Fábrica de ERROR: crea una respuesta fallida sin datos
    // — Ejemplo: ApiResponse<object>.Failure("No encontrado")
    // — errores es opcional: se usa cuando hay múltiples errores de validación
    public static ApiResponse<T> Failure(string mensaje, List<string>? errores = null)
    {
        return new ApiResponse<T>
        {
            // — Marcamos como fallido
            Exitoso = false,
            // — El mensaje de error principal
            Mensaje = mensaje,
            // — Lista de errores detallados (vacía si no se proporcionan)
            Errores = errores ?? new List<string>(),
            // — Momento del error
            Timestamp = DateTime.UtcNow
        };
    }
}
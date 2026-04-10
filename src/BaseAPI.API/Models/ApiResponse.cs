using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BaseAPI.API.Models
{
    public class ApiResponse <T>
    {

        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = string.Empty;


        //T?  = Puede ser null
        public T? Datos { get; set; }

        public List<string> Errores { get; set; } = new();

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;


        // Crea una respuesta exitosa con Datos
        public static ApiResponse <T> Success(T data, string? mensaje = null)
        {

            return new ApiResponse<T>
            {
                Exitoso = true,
                Mensaje = mensaje ?? "Operacion exitosa",
                Datos = data,
                Timestamp = DateTime.UtcNow

            };
        }

        public static ApiResponse<T> Failure (string mensaje, List<string>? errores = null)
        {

            return new ApiResponse<T>
            {
                Exitoso = false,
                Mensaje = mensaje,
                Errores = errores ?? new List<string>(),
                Timestamp = DateTime.UtcNow

            };

        }

    }
}

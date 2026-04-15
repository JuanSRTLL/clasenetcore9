// — Espacio de nombres compartido con ErrorCodes
namespace BaseAPI.Application.Common;

// ═══════════════════════════════════════════════════════════════
// CLASE ERROR — representa un error con código y mensaje
// ═══════════════════════════════════════════════════════════════
// — "record" es un tipo especial de C# que es INMUTABLE (no se puede modificar después de crearlo)
// — Es ideal para valores que representan algo fijo, como un error
// — Tiene Code (código del error) y Message (descripción para el usuario)
public record Error(string Code, string Message)
{
    // — Error vacío: se usa internamente cuando el resultado es EXITOSO (no hay error real)
    public static Error None = new(string.Empty, string.Empty);

    // ═══════════════════════════════════════════════════════════════
    // MÉTODOS FÁBRICA — crean errores tipados con códigos predefinidos
    // ═══════════════════════════════════════════════════════════════

    // — Crea un error de tipo "no encontrado" usando la constante ErrorCodes.NotFound
    // — En la capa API se convertirá en HTTP 404
    public static Error NotFound(string message) =>
        new(ErrorCodes.NotFound, message);

    // — Crea un error de tipo "validación" usando la constante ErrorCodes.Validation
    // — En la capa API se convertirá en HTTP 400
    public static Error Validation(string message) =>
        new(ErrorCodes.Validation, message);

    // — Crea un error de tipo "error interno" usando la constante ErrorCodes.InternalError
    // — En la capa API se convertirá en HTTP 500
    public static Error Internal(string message) =>
        new(ErrorCodes.InternalError, message);

    // — Crea un error de tipo "conflicto" usando la constante ErrorCodes.Conflict
    // — En la capa API se convertirá en HTTP 409
    public static Error Conflict(string message) =>
        new(ErrorCodes.Conflict, message);
}

// ═══════════════════════════════════════════════════════════════
// CLASE Result<T> — Resultado CON valor de retorno
// ═══════════════════════════════════════════════════════════════
// — T es el tipo de dato que retornamos cuando la operación es exitosa
// — Ejemplo: Result<List<EstudianteDto>> → si es éxito, contiene la lista de estudiantes
public class Result<T>
{
    // — Propiedad que indica si la operación fue exitosa (true) o falló (false)
    public bool IsSuccess { get; }
    // — Propiedad de conveniencia: es lo opuesto a IsSuccess
    public bool IsFailure => !IsSuccess;
    // — El valor de retorno cuando la operación fue exitosa (null si falló)
    public T? Value { get; }
    // — El error que ocurrió cuando la operación falló (Error.None si fue exitosa)
    public Error Error { get; }

    // — Constructor PRIVADO: no se puede llamar desde fuera de esta clase
    // — Esto obliga a usar los métodos Success() o Failure() para crear instancias
    private Result(bool isSuccess, T? value, Error error)
    {
        // — Guardamos si fue éxito o fallo
        IsSuccess = isSuccess;
        // — Guardamos el valor (solo tiene sentido si fue éxito)
        Value = value;
        // — Guardamos el error (solo tiene sentido si fue fallo)
        Error = error;
    }

    // — Método estático que crea un resultado EXITOSO con el valor proporcionado
    // — Ejemplo: return Result<EstudianteDto>.Success(estudiante);
    public static Result<T> Success(T value) => new(true, value, Error.None);

    // — Método estático que crea un resultado FALLIDO con el error proporcionado
    // — Ejemplo: return Result<EstudianteDto>.Failure(Error.NotFound("No existe"));
    public static Result<T> Failure(Error error) => new(false, default, error);

    // — CONVERSIÓN IMPLÍCITA: permite escribir "return estudiante;" en un método Result<EstudianteDto>
    // — C# automáticamente lo convierte en Result<EstudianteDto>.Success(estudiante)
    public static implicit operator Result<T>(T value) => Success(value);

    // — CONVERSIÓN IMPLÍCITA: permite escribir "return Error.NotFound(msg);" en un método Result<T>
    // — C# automáticamente lo convierte en Result<T>.Failure(error)
    public static implicit operator Result<T>(Error error) => Failure(error);
}

// ═══════════════════════════════════════════════════════════════
// CLASE Result (sin tipo) — Resultado SIN valor de retorno
// ═══════════════════════════════════════════════════════════════
// — Se usa cuando la operación solo necesita indicar éxito o fallo, sin retornar datos
// — Ejemplo: "Crear estudiante" → solo nos importa si se creó o no, no retornamos un objeto
public class Result
{
    // — Indica si la operación fue exitosa
    public bool IsSuccess { get; }
    // — Conveniencia: lo opuesto a IsSuccess
    public bool IsFailure => !IsSuccess;
    // — El error que ocurrió (Error.None si fue exitosa)
    public Error Error { get; }

    // — Constructor privado: obliga a usar Success() o Failure()
    private Result(bool isSuccess, Error error)
    {
        // — Guardamos si fue éxito o fallo
        IsSuccess = isSuccess;
        // — Guardamos el error
        Error = error;
    }

    // — Crea un resultado exitoso (sin valor, solo el hecho de que fue éxito)
    public static Result Success() => new(true, Error.None);

    // — Crea un resultado fallido con el error proporcionado
    public static Result Failure(Error error) => new(false, error);

    // — Conversión implícita: permite escribir "return Error.NotFound(msg);" directamente
    public static implicit operator Result(Error error) => Failure(error);
}

// — Importamos la interfaz del repositorio (definida en Application, Clase 2)
// !! IEstudiantesRepository (Clase 2) — contrato que esta clase implementa
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
// — Importamos el DTO que el Handler espera recibir (definido en Application, Clase 2)
// !! EstudianteDto (Clase 2) — el modelo público que el Handler y Controller usan
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
// — Importamos las clases Procedure que ejecutan los stored procedures de Oracle
// !! ListarEstudiantesProcedure (sección 1) — ejecuta PRO_LISTAR_ESTUDIANTES
// !! CrearEstudianteProcedure (sección 2) — ejecuta PRO_CREAR_ESTUDIANTE
using BaseAPI.Infrastructure.Features.Estudiantes.Procedures;

// — Espacio de nombres: Feature → Estudiantes (sin /Procedures, está un nivel arriba)
namespace BaseAPI.Infrastructure.Features.Estudiantes;

// ═══════════════════════════════════════════════════════════════
// — EstudiantesRepository: implementación concreta del contrato
// ═══════════════════════════════════════════════════════════════
// !! IEstudiantesRepository (Clase 2, Application) — la interfaz que implementamos
// — El Repository tiene 2 responsabilidades:
// —   1. Delegar la ejecución al Procedure correspondiente
// —   2. Convertir el resultado de formato Oracle → formato Application (OracleRow → Dto)
// ═══════════════════════════════════════════════════════════════
public class EstudiantesRepository : IEstudiantesRepository
{
    // — _listar: el procedure que consulta todos los estudiantes en Oracle
    // !! ListarEstudiantesProcedure (sección 1)
    private readonly ListarEstudiantesProcedure _listar;
    // — _crear: el procedure que inserta un nuevo estudiante en Oracle
    // !! CrearEstudianteProcedure (sección 2)
    private readonly CrearEstudianteProcedure _crear;

    // — Constructor: recibe los Procedures por DI
    // — DI los inyecta porque los registramos en la sección 7:
    // —   services.AddScoped<ListarEstudiantesProcedure>();
    // —   services.AddScoped<CrearEstudianteProcedure>();
    public EstudiantesRepository(
        ListarEstudiantesProcedure listar,
        CrearEstudianteProcedure crear)
    {
        _listar = listar;
        _crear = crear;
    }

    // ═══════════════════════════════════════════════════════════════
    // — ListarEstudiantesAsync: lista todos los estudiantes
    // ═══════════════════════════════════════════════════════════════
    // !! IEstudiantesRepository.ListarEstudiantesAsync() — método del contrato (Clase 2)
    // — Retorna List<EstudianteDto>: el DTO público que usa Application
    // — Internamente trabaja con EstudianteOracleRow y lo convierte a DTO
    public async Task<List<EstudianteDto>> ListarEstudiantesAsync(
        CancellationToken cancellationToken = default)
    {
        // — Paso 1: ejecutar el procedure Oracle via ListarEstudiantesProcedure
        // — rows es List<EstudianteOracleRow> (modelo INTERNO de Infrastructure)
        var rows = await _listar.ExecuteAsync(cancellationToken);

        // — Paso 2: convertir cada OracleRow → Dto
        // — .Select(): LINQ — para CADA fila del resultado, ejecuta la función lambda
        // — row => new EstudianteDto { ... }: crea un DTO nuevo y copia las propiedades
        // — .ToList(): convierte el IEnumerable<EstudianteDto> resultado en List<EstudianteDto>
        return rows.Select(row => new EstudianteDto
        {
            // — Copiamos cada propiedad del OracleRow al Dto
            // — Los nombres pueden ser iguales, pero son OBJETOS DIFERENTES:
            // —   row.IdEstudiante (EstudianteOracleRow) → dto.IdEstudiante (EstudianteDto)
            IdEstudiante = row.IdEstudiante,
            Nombre = row.Nombre,
            Apellido = row.Apellido,
            Identificacion = row.Identificacion,
            Correo = row.Correo,
            Programa = row.Programa,
            AnioMatricula = row.AnioMatricula,
            FechaRegistro = row.FechaRegistro
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // — CrearEstudianteAsync: inserta un nuevo estudiante
    // ═══════════════════════════════════════════════════════════════
    // !! IEstudiantesRepository.CrearEstudianteAsync() — método del contrato (Clase 2)
    // — Retorna string: "OK" o "ERROR: ..."
    // — No necesita conversión OracleRow → Dto porque el resultado ya es un string
    public async Task<string> CrearEstudianteAsync(
        string nombre, string apellido, string identificacion,
        string? correo, string programa, int anioMatricula,
        CancellationToken cancellationToken = default)
    {
        // — Delega directamente al Procedure sin conversión
        // — Los parámetros se pasan tal cual al procedure de Oracle
        return await _crear.ExecuteAsync(
            nombre, apellido, identificacion, correo, programa, anioMatricula, cancellationToken);
    }
}
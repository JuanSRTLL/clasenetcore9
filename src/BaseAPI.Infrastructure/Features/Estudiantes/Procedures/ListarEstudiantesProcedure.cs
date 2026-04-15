// — Importamos OracleParameter y OracleDbType para armar los parámetros del procedure
using Oracle.ManagedDataAccess.Client;
// — Importamos ParameterDirection para indicar si un parámetro es IN, OUT o ambos
using System.Data;
// — Importamos IOracleExecutor: la interfaz del motor Oracle que ejecuta procedures
// !! IOracleExecutor (Clase 1, repo base) — tiene ExecuteCursorAsync<T>() y ExecuteWithOutputAsync()
using BaseAPI.Infrastructure.Services.Oracle.Core;
// — Importamos OracleColumnAttribute para decorar las propiedades del modelo OracleRow
// !! [OracleColumn("NOMBRE_COLUMNA")] (Clase 1, repo base) — OracleMapper lo usa para mapear
using BaseAPI.Infrastructure.Services.Oracle.Core.Attributes;

// — Espacio de nombres: organizado por Feature → Estudiantes → Procedures
// — Siguiendo Vertical Slice Architecture (Clase 2):
// —   todo lo de "Estudiantes-Infrastructure" está junto en esta carpeta
namespace BaseAPI.Infrastructure.Features.Estudiantes.Procedures;

// ═══════════════════════════════════════════════════════════════
// — EstudianteOracleRow: modelo INTERNO que representa UNA FILA del cursor
// ═══════════════════════════════════════════════════════════════
// — ¿Por qué "OracleRow" y no "Dto"?
// —   Porque este modelo es INTERNO de Infrastructure.
// —   Solo lo usa ListarEstudiantesProcedure y EstudiantesRepository.
// —   El mundo exterior (Application, API) usa EstudianteDto (Clase 2).
// — ¿Cómo funciona el mapeo?
// —   1. Oracle retorna un cursor con columnas: ID_ESTUDIANTE, NOMBRE, APELLIDO...
// —   2. OracleMapper (Clase 1) lee cada fila del cursor
// —   3. Para cada propiedad, busca el [OracleColumn("NOMBRE_COLUMNA")]
// —   4. Lee el valor de esa columna y lo asigna a la propiedad
// ═══════════════════════════════════════════════════════════════
public class EstudianteOracleRow
{
    // — [OracleColumn("ID_ESTUDIANTE")]: "cuando leas la columna ID_ESTUDIANTE del cursor, pon el valor aquí"
    // !! OracleColumnAttribute (Clase 1, repo base) — le dice al OracleMapper qué columna mapear
    [OracleColumn("ID_ESTUDIANTE")]
    // — Propiedad C# que recibirá el valor de la columna ID_ESTUDIANTE
    public int IdEstudiante { get; set; }

    [OracleColumn("NOMBRE")]
    // — string.Empty: valor por defecto para evitar null en strings obligatorios
    public string Nombre { get; set; } = string.Empty;

    [OracleColumn("APELLIDO")]
    public string Apellido { get; set; } = string.Empty;

    [OracleColumn("IDENTIFICACION")]
    public string Identificacion { get; set; } = string.Empty;

    [OracleColumn("CORREO")]
    // — string? (nullable): el correo puede ser NULL en la BD
    // — Cuando la columna Oracle tiene NULL, OracleMapper asigna null a esta propiedad
    public string? Correo { get; set; }

    [OracleColumn("PROGRAMA")]
    public string Programa { get; set; } = string.Empty;

    [OracleColumn("ANIO_MATRICULA")]
    public int AnioMatricula { get; set; }

    [OracleColumn("FECHA_REGISTRO")]
    public DateTime FechaRegistro { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// — ListarEstudiantesProcedure: clase que ejecuta el procedure de Oracle
// ═══════════════════════════════════════════════════════════════
// — PATRÓN: cada procedure de Oracle tiene su propia clase C#
// — Esto permite:
// —   1. Aislar la lógica de parámetros Oracle de la lógica de negocio
// —   2. Reutilizar: si otro feature necesita listar estudiantes, usa esta misma clase
// —   3. Testear: se puede mockear IOracleExecutor para pruebas unitarias
// ═══════════════════════════════════════════════════════════════
public class ListarEstudiantesProcedure
{
    // — Constantes: nombre del paquete y del procedure en Oracle
    // — Se usan en ExecuteCursorAsync() para armar el comando:
    // —   "PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES"
    private const string PackageName = "PAQ_ESTUDIANTES";
    private const string ProcedureName = "PRO_LISTAR_ESTUDIANTES";

    // — _executor: el motor Oracle que ejecuta stored procedures
    // !! IOracleExecutor (Clase 1, repo base) — inyectado por DI
    // — ¿Por qué IOracleExecutor (interfaz) y no OracleExecutor (clase)?
    // —   Porque dependemos del CONTRATO, no de la implementación.
    // —   En pruebas se puede inyectar un MockExecutor.
    private readonly IOracleExecutor _executor;

    // — Constructor: recibe IOracleExecutor por DI
    // — DI lo inyecta porque lo registramos en Clase 4 sección 6:
    // —   services.AddScoped<IOracleExecutor, OracleExecutor>();
    public ListarEstudiantesProcedure(IOracleExecutor executor)
    {
        _executor = executor;
    }

    // — ExecuteAsync: ejecuta el procedure y retorna la lista de estudiantes
    // — Retorna Task<List<EstudianteOracleRow>>: lista de filas del cursor
    // — CancellationToken: permite cancelar la operación si el request se cierra
    public async Task<List<EstudianteOracleRow>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        // — Armamos el array de parámetros Oracle
        // — PRO_LISTAR_ESTUDIANTES solo tiene UN parámetro: P_CURSOR (OUT)
        var parameters = new[]
        {
            // — P_CURSOR: parámetro de SALIDA de tipo RefCursor
            // — OracleDbType.RefCursor: tipo especial de Oracle que representa un cursor
            // — ParameterDirection.Output: Oracle ESCRIBE el cursor aquí, .NET lo LEE
            // — No enviamos valor porque es de SALIDA (Oracle lo llena)
            new OracleParameter("P_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
        };

        // — ExecuteCursorAsync<EstudianteOracleRow>:
        // —   1. Arma el comando: "PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES"
        // —   2. Obtiene una conexión Oracle (via GetConnectionContextAsync)
        // —   3. Ejecuta el procedure
        // —   4. Lee el cursor P_CURSOR fila por fila
        // —   5. Para cada fila, crea un EstudianteOracleRow y mapea las columnas
        // —      usando [OracleColumn] → OracleMapper (Clase 1)
        // —   6. Retorna List<EstudianteOracleRow> con todas las filas
        // !! IOracleExecutor.ExecuteCursorAsync<T> (Clase 1, repo base)
        return await _executor.ExecuteCursorAsync<EstudianteOracleRow>(
            PackageName, ProcedureName, parameters, cancellationToken);
    }
}
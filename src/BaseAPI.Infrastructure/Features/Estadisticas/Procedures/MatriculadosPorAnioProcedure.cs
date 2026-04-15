// — Importamos OracleParameter y OracleDbType para los parámetros del procedure
using Oracle.ManagedDataAccess.Client;
// — Importamos ParameterDirection para indicar Input/Output
using System.Data;
// — Importamos IOracleExecutor para ejecutar el procedure
// !! IOracleExecutor (Clase 1, repo base) — ExecuteCursorAsync<T>()
using BaseAPI.Infrastructure.Services.Oracle.Core;
// — Importamos OracleColumnAttribute para mapear columnas del cursor
// !! [OracleColumn] (Clase 1, repo base)
using BaseAPI.Infrastructure.Services.Oracle.Core.Attributes;

// — Espacio de nombres: Feature → Estadisticas → Procedures
namespace BaseAPI.Infrastructure.Features.Estadisticas.Procedures;

// ═══════════════════════════════════════════════════════════════
// — MatriculadosPorAnioOracleRow: modelo para una fila del cursor
// ═══════════════════════════════════════════════════════════════
// — Solo 2 columnas: ANIO y CANTIDAD
// — Corresponden a los alias del SELECT en Oracle:
// —   SELECT ANIO_MATRICULA AS ANIO, COUNT(*) AS CANTIDAD
// — El nombre en [OracleColumn] DEBE coincidir con el alias del SELECT
// ═══════════════════════════════════════════════════════════════
public class MatriculadosPorAnioOracleRow
{
    // — "ANIO": alias de ANIO_MATRICULA en el SELECT del procedure
    // — ¿Por qué alias? Porque el SELECT dice: ANIO_MATRICULA AS ANIO
    // — Si el SELECT dijera solo ANIO_MATRICULA, aquí sería [OracleColumn("ANIO_MATRICULA")]
    [OracleColumn("ANIO")]
    public int Anio { get; set; }

    // — "CANTIDAD": alias de COUNT(*) en el SELECT del procedure
    // — COUNT(*) no tiene nombre propio, el alias AS CANTIDAD le da uno
    [OracleColumn("CANTIDAD")]
    public int Cantidad { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// — MatriculadosPorAnioProcedure: ejecuta PRO_MATRICULADOS_POR_ANIO
// ═══════════════════════════════════════════════════════════════
// — Mismo patrón que ListarEstudiantesProcedure (sección 1):
// —   constantes + IOracleExecutor + ExecuteAsync + ExecuteCursorAsync
// — Diferencia: el OracleRow tiene solo 2 propiedades (ANIO, CANTIDAD)
// ═══════════════════════════════════════════════════════════════
public class MatriculadosPorAnioProcedure
{
    // — Paquete diferente a PAQ_ESTUDIANTES: este es PAQ_ESTADISTICAS
    private const string PackageName = "PAQ_ESTADISTICAS";
    private const string ProcedureName = "PRO_MATRICULADOS_POR_ANIO";

    // !! IOracleExecutor (Clase 1, repo base)
    private readonly IOracleExecutor _executor;

    public MatriculadosPorAnioProcedure(IOracleExecutor executor)
    {
        _executor = executor;
    }

    // — ExecuteAsync: ejecuta el procedure y retorna la lista de matriculados por año
    // — No tiene parámetros de entrada (el procedure lista TODOS los años)
    public async Task<List<MatriculadosPorAnioOracleRow>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        // — Solo un parámetro: P_CURSOR de salida (igual que ListarEstudiantesProcedure)
        var parameters = new[]
        {
            new OracleParameter("P_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
        };

        // — ExecuteCursorAsync: ejecuta el procedure, lee el cursor, mapea a OracleRow
        // — Con los 12 datos de prueba retornará:
        // —   [ {Anio=2023, Cantidad=2}, {Anio=2024, Cantidad=3},
        // —     {Anio=2025, Cantidad=4}, {Anio=2026, Cantidad=3} ]
        return await _executor.ExecuteCursorAsync<MatriculadosPorAnioOracleRow>(
            PackageName, ProcedureName, parameters, cancellationToken);
    }
}
// — Importamos OracleParameter y OracleDbType para los parámetros del procedure
using Oracle.ManagedDataAccess.Client;
// — Importamos ParameterDirection (Input/Output)
using System.Data;
// — Importamos IOracleExecutor para ejecutar el procedure
// !! IOracleExecutor (Clase 1, repo base)
using BaseAPI.Infrastructure.Services.Oracle.Core;
// — Importamos OracleColumnAttribute para el modelo OracleRow
// !! [OracleColumn] (Clase 1, repo base)
using BaseAPI.Infrastructure.Services.Oracle.Core.Attributes;

// — Espacio de nombres: misma carpeta que MatriculadosPorAnioProcedure
namespace BaseAPI.Infrastructure.Features.Estadisticas.Procedures;

// ═══════════════════════════════════════════════════════════════
// — MatriculadosPorProgramaOracleRow: modelo para una fila del cursor
// ═══════════════════════════════════════════════════════════════
// — Columnas: PROGRAMA (texto) y CANTIDAD (número)
// — Corresponden al SELECT: SELECT PROGRAMA, COUNT(*) AS CANTIDAD
// ═══════════════════════════════════════════════════════════════
public class MatriculadosPorProgramaOracleRow
{
    // — "PROGRAMA": nombre directo de la columna (no tiene alias en el SELECT)
    [OracleColumn("PROGRAMA")]
    public string Programa { get; set; } = string.Empty;

    // — "CANTIDAD": alias de COUNT(*) en el SELECT
    [OracleColumn("CANTIDAD")]
    public int Cantidad { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// — MatriculadosPorProgramaProcedure: ejecuta PRO_MATRICULADOS_POR_PROGRAMA
// ═══════════════════════════════════════════════════════════════
// — DIFERENCIA con MatriculadosPorAnioProcedure (sección 3):
// —   ↑ No recibe parámetros IN (lista todos los años)
// —   → Este SÍ recibe un parámetro IN: P_ANIO (filtra por año)
// — Es el ÚNICO procedure de estadísticas que recibe un parámetro de entrada
// ═══════════════════════════════════════════════════════════════
public class MatriculadosPorProgramaProcedure
{
    private const string PackageName = "PAQ_ESTADISTICAS";
    private const string ProcedureName = "PRO_MATRICULADOS_POR_PROGRAMA";

    // !! IOracleExecutor (Clase 1, repo base)
    private readonly IOracleExecutor _executor;

    public MatriculadosPorProgramaProcedure(IOracleExecutor executor)
    {
        _executor = executor;
    }

    // — ExecuteAsync: recibe el año como parámetro y retorna los programas de ese año
    // — int anio: el año a filtrar (ej: 2025)
    public async Task<List<MatriculadosPorProgramaOracleRow>> ExecuteAsync(
        int anio, CancellationToken cancellationToken = default)
    {
        // — ESTE procedure tiene 2 parámetros: 1 IN + 1 OUT
        var parameters = new[]
        {
            // — P_ANIO: parámetro de ENTRADA — el año que queremos filtrar
            // — OracleDbType.Int32: tipo entero (coincide con NUMBER en Oracle)
            // — anio: el valor que viene del Handler (ej: 2025)
            // — ParameterDirection.Input: .NET ENVÍA este valor, Oracle lo LEE
            new OracleParameter("P_ANIO", OracleDbType.Int32, anio, ParameterDirection.Input),
            // — P_CURSOR: parámetro de SALIDA — Oracle escribe aquí el resultado
            new OracleParameter("P_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output)
        };

        // — ExecuteCursorAsync: ejecuta el procedure y mapea el cursor a OracleRow
        // — Para anio=2025 con los datos de prueba retornará:
        // —   [ {Programa="Administración de Empresas", Cantidad=1},
        // —     {Programa="Contaduría Pública", Cantidad=1},
        // —     {Programa="Derecho", Cantidad=1},
        // —     {Programa="Ingeniería de Sistemas", Cantidad=1} ]
        return await _executor.ExecuteCursorAsync<MatriculadosPorProgramaOracleRow>(
            PackageName, ProcedureName, parameters, cancellationToken);
    }
}
// — Importamos OracleParameter y OracleDbType para armar los parámetros del procedure
using Oracle.ManagedDataAccess.Client;
// — Importamos ParameterDirection: indica si cada parámetro es IN (enviar) u OUT (recibir)
using System.Data;
// — Importamos IOracleExecutor: el motor que ejecuta los stored procedures
// !! IOracleExecutor (Clase 1, repo base) — tiene ExecuteWithOutputAsync()
using BaseAPI.Infrastructure.Services.Oracle.Core;

// — Mismo namespace que ListarEstudiantesProcedure (misma carpeta)
namespace BaseAPI.Infrastructure.Features.Estudiantes.Procedures;

// ═══════════════════════════════════════════════════════════════
// — CrearEstudianteProcedure: ejecuta PRO_CREAR_ESTUDIANTE
// ═══════════════════════════════════════════════════════════════
// — DIFERENCIA con ListarEstudiantesProcedure:
// —   Listar → ExecuteCursorAsync<T>()  → lee un CURSOR (muchas filas)
// —   Crear  → ExecuteWithOutputAsync() → lee un VALOR de salida (un string)
// ═══════════════════════════════════════════════════════════════
public class CrearEstudianteProcedure
{
    // — Constantes del paquete y procedure Oracle
    private const string PackageName = "PAQ_ESTUDIANTES";
    private const string ProcedureName = "PRO_CREAR_ESTUDIANTE";

    // !! IOracleExecutor (Clase 1, repo base) — inyectado por DI
    private readonly IOracleExecutor _executor;

    public CrearEstudianteProcedure(IOracleExecutor executor)
    {
        _executor = executor;
    }

    // — ExecuteAsync: ejecuta el procedure y retorna el resultado como string
    // — Recibe todos los datos del estudiante como parámetros
    // — Retorna "OK" si se creó, o "ERROR: ..." si falló
    public async Task<string> ExecuteAsync(
        string nombre, string apellido, string identificacion,
        string? correo, string programa, int anioMatricula,
        CancellationToken cancellationToken = default)
    {
        // — Armamos los 7 parámetros del procedure (6 IN + 1 OUT)
        var parameters = new[]
        {
            // — Parámetros de ENTRADA (ParameterDirection.Input):
            // — Oracle recibe estos valores para insertar en la tabla
            // — OracleDbType.Varchar2: tipo texto de Oracle (equivalente a string en C#)
            new OracleParameter("P_NOMBRE", OracleDbType.Varchar2, nombre, ParameterDirection.Input),
            new OracleParameter("P_APELLIDO", OracleDbType.Varchar2, apellido, ParameterDirection.Input),
            new OracleParameter("P_IDENTIFICACION", OracleDbType.Varchar2, identificacion, ParameterDirection.Input),
            // — correo ?? (object)DBNull.Value: si correo es null en C#, se envía DBNull a Oracle
            // — DBNull.Value: valor especial de .NET que representa NULL en base de datos
            // — (object) cast: necesario porque OracleParameter espera object, no string?
            // — ¿Por qué no enviar null directamente? Porque OracleParameter interpretaría null como
            // —   "no se proporcionó valor" en vez de "el valor es NULL en la BD"
            new OracleParameter("P_CORREO", OracleDbType.Varchar2, correo ?? (object)DBNull.Value, ParameterDirection.Input),
            new OracleParameter("P_PROGRAMA", OracleDbType.Varchar2, programa, ParameterDirection.Input),
            // — OracleDbType.Int32: tipo número entero de Oracle
            new OracleParameter("P_ANIO_MATRICULA", OracleDbType.Int32, anioMatricula, ParameterDirection.Input),
            // — P_RESULTADO: parámetro de SALIDA donde Oracle escribe "OK" o "ERROR: ..."
            // — 500: tamaño máximo del string de salida (500 caracteres)
            // — "": valor inicial vacío (Oracle lo sobreescribe)
            // — ParameterDirection.Output: .NET LEE este valor después de ejecutar
            new OracleParameter("P_RESULTADO", OracleDbType.Varchar2, 500, "", ParameterDirection.Output)
        };

        // — ExecuteWithOutputAsync: ejecuta el procedure y lee el valor de P_RESULTADO
        // !! IOracleExecutor.ExecuteWithOutputAsync (Clase 1, repo base)
        // — Argumentos:
        // —   PackageName: "PAQ_ESTUDIANTES"
        // —   ProcedureName: "PRO_CREAR_ESTUDIANTE"
        // —   parameters: los 7 parámetros
        // —   "P_RESULTADO": nombre del parámetro OUT que queremos leer
        // — Retorna: el valor de P_RESULTADO como string (ej: "OK")
        var resultado = await _executor.ExecuteWithOutputAsync(
            PackageName, ProcedureName, parameters, "P_RESULTADO", cancellationToken);

        // — Si resultado es null (no debería pasar), retornamos un error genérico
        // — El ?? operador: "si resultado es null, usa el string de la derecha"
        return resultado ?? "ERROR: Sin respuesta del procedimiento";
    }
}
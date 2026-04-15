// Importamos  para armar los parametros del procedure
using Oracle.ManagedDataAccess.Client;
// Importamos paara indicar si cada parametro es IN(enviar) o un OUT (recibir)
using BaseAPI.Infrastructure.Services.Oracle.Core;

using System.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseAPI.Infrastructure.Features.Procedures
{
    public class CrearEstudianteProcedure
    {
        private const string PackageName = "PAQ_ESTUDIANTES";
        private const string ProcedureName = "PRO_CREAR_ESTUDIANTE";

        private readonly IOracleExecutor _executor;
        public CrearEstudianteProcedure(IOracleExecutor executor)
        {
            _executor = executor;
        }
        public async Task<string> ExecuteAsync(
            string nombre, string apellido, string identificacion, 
            string? correo, string programa, int anioMatricula,
            CancellationToken cancellationToken
            )
        {
            var parameters = new[]
            {
                new OracleParameter("P_NOMBRE", OracleDbType.Varchar2, nombre, ParameterDirection.Input),
                new OracleParameter("P_APELLIDO", OracleDbType.Varchar2, apellido, ParameterDirection.Input),
                new OracleParameter("P_IDENTIFICACION", OracleDbType.Varchar2, identificacion, ParameterDirection.Input),
                new OracleParameter("P_CORREO", OracleDbType.Varchar2, correo?? (object)DBNull.Value, ParameterDirection.Input),
                new OracleParameter("P_PROGRAMA", OracleDbType.Varchar2, programa, ParameterDirection.Input),
                new OracleParameter("P_ANIO_MATRICULA", OracleDbType.Int32, anioMatricula, ParameterDirection.Input),
                new OracleParameter("P_RESULTADO", OracleDbType.Varchar2, 500, "", ParameterDirection.Output)
            };
            var resultado = await _executor.ExecuteWithOutputAsync(
                   PackageName, ProcedureName, parameters, "P_RESULTADO", cancellationToken
                   );

            return resultado ?? "ERROR: Sin respuesta del procedimiento";
        }

    }
}

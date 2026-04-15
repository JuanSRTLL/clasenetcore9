using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseAPI.Application.Contracts.Persistence;
using BaseAPI.Infrastructure.Services.Oracle.Core;
using BaseAPI.Infrastructure.Services.Oracle.Core.Attributes;
using Oracle.ManagedDataAccess.Client;

namespace BaseAPI.Infrastructure.Features.Procedures
{
    public class EstudianteOracleRow
    {
        [OracleColumn("ID_ESTUDIANTE")]
        public int IdEstudiante {  get; set; }
        [OracleColumn("NOMBRE")]
        public string Nombre { get; set; } = string.Empty;
        [OracleColumn("APELLIDO")]
        public string Apellido { get; set; } = string.Empty;
        [OracleColumn("IDENTIFICACION")]
        public string Identificacion {  get; set; } = string.Empty;

        [OracleColumn("CORREO")]
        public string? Correo {  get; set; } = string.Empty;
        [OracleColumn("PROGRAMA")]
        public string Programa { get; set; } = string.Empty;
        [OracleColumn("ANIO_MATRICULA")]
        public int AnioMatricula { get; set; }

        [OracleColumn("FECHA_REGISTRO")]
        public DateTime FechaRegistro { get; set; }

    }

    public class ListarEstudiantesProcedure
    {
        private const string PackageName = "PAQ_ESTUDIANTES";
        private const string ProcedureName = "PRO_LISTAR_ESTUDIANTES";

        private readonly IOracleExecutor _executor;

        public ListarEstudiantesProcedure(IOracleExecutor executor)
        {
            _executor = executor;

        }
        public async Task<List<EstudianteOracleRow>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        {

        var parameters = new[]
        {
            new OracleParameter("P_CURSOR", OracleDbType.RefCursor, System.Data.ParameterDirection.Output)
        };

            return await _executor.ExecuteCursorAsync<EstudianteOracleRow>(
                PackageName, ProcedureName, parameters, cancellationToken
                );
        }
    }
    
}

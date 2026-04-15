using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;

namespace BaseAPI.Application.Features.Estudiantes._Shared.Contracts
{
    public interface IEstudianteRepository
    {

        //Posteriormente se utilizara en infraestructura para llamar a  PAQ_ESTUDIANTES.PRO_LISTAR_ESTUDIANTES
        Task<List<EstudiantesDTO>> ListarEstudiantesAsync(CancellationToken cancellationToken = default);

        //Posteriormente se utilizara en infraestructura para llamar a  PAQ_ESTUDIANTES.PRO_CREAR_ESTUDIANTE

        Task<string> CrearEstudianteAsync(
            string nombre, string apellido, string identificacion,
            string? correo,string programa, int anioMatricula,
            CancellationToken cancellationToken = default);
        //Task<Result<List<EstudiantesDTO>>> ListarEstudiantesAsync(object cancellationToken);
    }
}

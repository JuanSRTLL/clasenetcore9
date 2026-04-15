using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using BaseAPI.Infrastructure.Features.Procedures;

namespace BaseAPI.Infrastructure.Features.Estudiantes
{
    public class EstudiantesRepository : IEstudianteRepository
    {
        private readonly ListarEstudiantesProcedure _listar;
        private readonly CrearEstudianteProcedure _crear;

        public EstudiantesRepository(
            ListarEstudiantesProcedure listar,
            CrearEstudianteProcedure crear)
        {
            _listar = listar;
            _crear = crear;
        }
        public async Task<List<EstudiantesDTO>> ListarEstudiantesAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _listar.ExecuteAsync(cancellationToken);
            return rows.Select(row => new EstudiantesDTO{
                IdEstudiante = row.IdEstudiante,
                Nombre = row.Nombre,
                Apellido = row.Apellido,
                Identicacion = row.Identificacion,
                Correo = row.Correo,
                Programa = row.Programa,
                AnioMatricula = row.AnioMatricula,
                FechaRegistro = row.FechaRegistro,
            }).ToList();
        }
        public async Task<string> CrearEstudianteAsync(
            string nombre, string apellido, string identificacion,
            string? correo, string programa, int anioMatricula,
            CancellationToken cancellationToken=default)
        {
            return await _crear.ExecuteAsync(
                nombre, apellido, identificacion, correo, programa, anioMatricula, cancellationToken);
         
        }

    }
}

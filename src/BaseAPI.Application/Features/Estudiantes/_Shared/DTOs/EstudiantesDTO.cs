using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseAPI.Application.Features.Estudiantes._Shared.DTOs
{
    public class EstudiantesDTO
    {
        public int IdEstudiante {  get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Identicacion { get; set; }

        public string? Correo { get; set; }

        public string Programa { get; set; }

        public int AnioMatricula { get; set; }

        public DateTime FechaRegistro { get; set; }
    }

    public class CrearEstudianteRequestDTO
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Identicacion { get; set; }

        public string? Correo { get; set; }

        public string Programa { get; set; }

        public int AnioMatricula { get; set; }
    }

    public class CrearEstudianteResponseDTO 
    {
        public string Mensaje { get; set; }
    }
}

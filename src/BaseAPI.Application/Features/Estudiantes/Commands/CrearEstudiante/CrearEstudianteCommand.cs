using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;


namespace BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante
{
    public record CrearEstudianteCommand(
        string Nombre,
        string Apellido,
        string Identificacion,
        string? Correo,
        string Programa,
        int AnioMatricula
        ): IRequest<Result<CrearEstudianteResponseDTO>>;
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante
{
    public class CrearEstudianteHandler : IRequestHandler<CrearEstudianteCommand, Result<CrearEstudianteResponseDTO>>
    {
        private readonly IEstudianteRepository _repository;
        private readonly ILogger<CrearEstudianteHandler> _logger;

        public CrearEstudianteHandler (
            IEstudianteRepository repository,
            ILogger<CrearEstudianteHandler> logger )
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task <Result<CrearEstudianteResponseDTO>> Handle(
            CrearEstudianteCommand request,
            CancellationToken cancellationToken)
        {
            var resultado = await _repository.CrearEstudianteAsync(
                request.Nombre,
                request.Apellido,
                request.Identificacion,
                request.Correo,
                request.Programa,
                request.AnioMatricula,
                cancellationToken);

            if (resultado != "OK")
            {
                _logger.LogInformation("Error al crear estudiante {Identificacion}: {Resultado}",
                    request.Identificacion, resultado);

                return Error.Conflict(resultado.Replace("Error ", ""));
            }
            _logger.LogInformation("Estudiante creado: {Nombre} {Apellido} ({Identificacion})",
                request.Nombre, request.Apellido, request.Identificacion);

            return new CrearEstudianteResponseDTO
            {
                Mensaje = "Estudiante creado exitosamente"

            };

        }
            

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaseAPI.Application.Common;
using BaseAPI.Application.Features.Estudiantes._Shared.Contracts;
using BaseAPI.Application.Features.Estudiantes._Shared.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BaseAPI.Application.Features.Estudiantes.Queries.ListarEstudiantes
{
    public class ListarEstudiantesHandler :IRequestHandler<ListarEstudiantesQuery, Result<List<EstudiantesDTO>>>
    {
        private readonly IEstudianteRepository _repository;
        private readonly ILogger<ListarEstudiantesHandler> _logger;


        public ListarEstudiantesHandler(
            IEstudianteRepository repository,
            ILogger<ListarEstudiantesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<List<EstudiantesDTO>>> Handle(
        ListarEstudiantesQuery request,
        CancellationToken cancellationToken)
        {
            var estudiante = await _repository.ListarEstudiantesAsync(cancellationToken);

            _logger.LogInformation("Se listaron {Count} estudiantes", estudiante.Count);

            return estudiante;
        }
    }  
}

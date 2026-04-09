using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante
{
    public class CrearEstudianteCommandValidator : AbstractValidator<CrearEstudianteCommand>
    {

        public CrearEstudianteCommandValidator() {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MaximumLength(100).WithMessage("El nombre no puede exceder  100 caracteres");
            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es obligatorio")
                .MaximumLength(100).WithMessage("El apellido no puede exceder  100 caracteres");
            RuleFor(x => x.Identificacion)
                .NotEmpty().WithMessage("El identificacion es obligatorio")
                .MaximumLength(20).WithMessage("El identificacion no puede exceder  20 caracteres");
            RuleFor(x => x.Programa)
                .NotEmpty().WithMessage("El programa es obligatorio")
                .MaximumLength(150).WithMessage("El programa no puede exceder  20 caracteres");
            RuleFor(x => x.AnioMatricula)
                .InclusiveBetween(2000, 2100).WithMessage("El año de matricula debe estar entre 2000 y 2100");

        }
        

    }
}

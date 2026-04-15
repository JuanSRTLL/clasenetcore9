using FluentValidation;

namespace BaseAPI.Application.Features.Estudiantes.Commands.CrearEstudiante;

/// <summary>
/// Validator para el CrearEstudianteCommand.
/// Garantiza que los datos del estudiante sean válidos antes de intentar la inserción.
/// </summary>
public class CrearEstudianteCommandValidator : AbstractValidator<CrearEstudianteCommand>
{
    public CrearEstudianteCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es obligatorio")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres");

        RuleFor(x => x.Identificacion)
            .NotEmpty().WithMessage("La identificación es obligatoria")
            .MaximumLength(20).WithMessage("La identificación no puede exceder 20 caracteres");

        RuleFor(x => x.Programa)
            .NotEmpty().WithMessage("El programa académico es obligatorio")
            .MaximumLength(150).WithMessage("El programa no puede exceder 150 caracteres");

        RuleFor(x => x.AnioMatricula)
            .InclusiveBetween(2000, 2100).WithMessage("El año de matrícula debe estar entre 2000 y 2100");
        
    }
}

using FluentValidation;
using Itinerario.Application.DTOs;

namespace Itinerario.Application.Validators;

public class BosquejoRequestDtoValidator : AbstractValidator<BosquejoRequestDto>
{
    public BosquejoRequestDtoValidator()
    {
        RuleFor(x => x.Texto)
            .NotEmpty()
            .MaximumLength(500);
    }
}

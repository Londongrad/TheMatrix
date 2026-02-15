using FluentValidation;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public sealed class KillPersonCommandValidator : AbstractValidator<KillPersonCommand>
    {
        public KillPersonCommandValidator()
        {
            RuleFor(x => x.Id)
               .NotEmpty();
        }
    }
}

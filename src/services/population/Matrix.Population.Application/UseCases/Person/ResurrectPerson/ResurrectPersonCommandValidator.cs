using FluentValidation;

namespace Matrix.Population.Application.UseCases.Person.ResurrectPerson
{
    public sealed class ResurrectPersonCommandValidator : AbstractValidator<ResurrectPersonCommand>
    {
        public ResurrectPersonCommandValidator()
        {
            RuleFor(x => x.Id)
               .NotEmpty();
        }
    }
}

using FluentValidation;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.UseCases.Person.UpdatePerson
{
    public sealed class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
    {
        public UpdatePersonCommandValidator()
        {
            RuleFor(x => x.Id)
               .NotEmpty();

            RuleFor(x => x.FullName)
               .MaximumLength(200)
               .When(x => x.FullName is not null);

            RuleFor(x => x.Health)
               .InclusiveBetween(HealthLevel.MinHealth, HealthLevel.MaxHealth)
               .When(x => x.Health.HasValue);

            RuleFor(x => x.Happiness)
               .InclusiveBetween(HappinessLevel.MinHappiness, HappinessLevel.MaxHappiness)
               .When(x => x.Happiness.HasValue);

            RuleFor(x => x.Energy)
               .InclusiveBetween(EnergyLevel.MinEnergy, EnergyLevel.MaxEnergy)
               .When(x => x.Energy.HasValue);

            RuleFor(x => x.Stress)
               .InclusiveBetween(StressLevel.MinStress, StressLevel.MaxStress)
               .When(x => x.Stress.HasValue);

            RuleFor(x => x.SocialNeed)
               .InclusiveBetween(SocialNeedLevel.MinSocialNeed, SocialNeedLevel.MaxSocialNeed)
               .When(x => x.SocialNeed.HasValue);

            RuleFor(x => x.EducationLevel)
               .Must(value => value is null || Enum.TryParse<EducationLevel>(value, ignoreCase: true, out _))
               .WithMessage("EducationLevel must be a valid population education level.");
        }
    }
}

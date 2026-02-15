using FluentValidation;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class InitializeCityPopulationCommandValidator : AbstractValidator<InitializeCityPopulationCommand>
    {
        public InitializeCityPopulationCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.PeopleCount)
               .GreaterThan(0);

            RuleFor(x => x.Environment)
               .NotNull()
               .SetValidator(new CityPopulationEnvironmentInputValidator());

            RuleFor(x => x.ResidentialBuildings)
               .NotNull();

            RuleForEach(x => x.ResidentialBuildings)
               .SetValidator(new ResidentialBuildingSeedItemValidator());
        }
    }
}

using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common
{
    public sealed class CityPopulationBootstrapTuningInputValidator
        : AbstractValidator<CityPopulationBootstrapTuningInput>
    {
        public CityPopulationBootstrapTuningInputValidator()
        {
            RuleFor(x => x.HousingPressurePercent)
               .InclusiveBetween(0, 100);

            RuleFor(x => x.EconomicStabilityPercent)
               .InclusiveBetween(0, 100);

            RuleFor(x => x.SocialVolatilityPercent)
               .InclusiveBetween(0, 100);

            RuleFor(x => x.FamilyFormationPercent)
               .InclusiveBetween(0, 100);
        }
    }
}

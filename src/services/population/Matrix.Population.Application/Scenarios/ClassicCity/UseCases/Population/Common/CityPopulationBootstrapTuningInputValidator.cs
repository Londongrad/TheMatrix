using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common
{
    public sealed class CityPopulationBootstrapTuningInputValidator
        : AbstractValidator<CityPopulationBootstrapTuningInput>
    {
        public CityPopulationBootstrapTuningInputValidator()
        {
            RuleFor(x => x.HousingPressurePercent)
               .InclusiveBetween(
                    from: 0,
                    to: 100);

            RuleFor(x => x.EconomicStabilityPercent)
               .InclusiveBetween(
                    from: 0,
                    to: 100);

            RuleFor(x => x.SocialVolatilityPercent)
               .InclusiveBetween(
                    from: 0,
                    to: 100);

            RuleFor(x => x.FamilyFormationPercent)
               .InclusiveBetween(
                    from: 0,
                    to: 100);
        }
    }
}

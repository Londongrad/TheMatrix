using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common
{
    public sealed class CityPopulationEnvironmentInputValidator : AbstractValidator<CityPopulationEnvironmentInput>
    {
        public CityPopulationEnvironmentInputValidator()
        {
            RuleFor(x => x.ClimateZone)
               .NotEmpty();

            RuleFor(x => x.Hemisphere)
               .NotEmpty();

            RuleFor(x => x.UtcOffsetMinutes)
               .InclusiveBetween(
                    from: -14 * 60,
                    to: 14 * 60);
        }
    }
}

using FluentValidation;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    DispatchCityPowerDistributionMaintenance
{
    public sealed class DispatchCityPowerDistributionMaintenanceCommandValidator
        : AbstractValidator<DispatchCityPowerDistributionMaintenanceCommand>
    {
        public DispatchCityPowerDistributionMaintenanceCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.Focus)
               .NotEmpty()
               .Must(BeValidFocus)
               .WithMessage("Focus is invalid.");

            RuleFor(x => x.Intensity)
               .NotEmpty()
               .Must(BeValidIntensity)
               .WithMessage("Intensity is invalid.");
        }

        private static bool BeValidFocus(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out PowerDistributionMaintenanceFocus focus) &&
                   Enum.IsDefined(focus);
        }

        private static bool BeValidIntensity(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out PowerDistributionMaintenanceIntensity intensity) &&
                   Enum.IsDefined(intensity);
        }
    }
}

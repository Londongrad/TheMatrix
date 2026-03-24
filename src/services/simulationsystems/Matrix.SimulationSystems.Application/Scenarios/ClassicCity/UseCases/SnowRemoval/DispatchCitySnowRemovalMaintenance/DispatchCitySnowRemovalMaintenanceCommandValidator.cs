using FluentValidation;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.DispatchCitySnowRemovalMaintenance
{
    public sealed class DispatchCitySnowRemovalMaintenanceCommandValidator
        : AbstractValidator<DispatchCitySnowRemovalMaintenanceCommand>
    {
        public DispatchCitySnowRemovalMaintenanceCommandValidator()
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
                       result: out SnowRemovalMaintenanceFocus focus) &&
                   Enum.IsDefined(focus);
        }

        private static bool BeValidIntensity(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out SnowRemovalMaintenanceIntensity intensity) &&
                   Enum.IsDefined(intensity);
        }
    }
}

using FluentValidation;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance
{
    public sealed class DispatchCitySanitationMaintenanceCommandValidator
        : AbstractValidator<DispatchCitySanitationMaintenanceCommand>
    {
        public DispatchCitySanitationMaintenanceCommandValidator()
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
                       result: out SanitationMaintenanceFocus focus) &&
                   Enum.IsDefined(focus);
        }

        private static bool BeValidIntensity(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out SanitationMaintenanceIntensity intensity) &&
                   Enum.IsDefined(intensity);
        }
    }
}

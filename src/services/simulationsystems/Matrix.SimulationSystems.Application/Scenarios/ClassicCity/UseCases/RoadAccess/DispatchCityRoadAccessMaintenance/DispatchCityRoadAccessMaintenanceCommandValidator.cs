using FluentValidation;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.
    DispatchCityRoadAccessMaintenance
{
    public sealed class DispatchCityRoadAccessMaintenanceCommandValidator
        : AbstractValidator<DispatchCityRoadAccessMaintenanceCommand>
    {
        public DispatchCityRoadAccessMaintenanceCommandValidator()
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
                       result: out RoadAccessMaintenanceFocus focus) &&
                   Enum.IsDefined(focus);
        }

        private static bool BeValidIntensity(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out RoadAccessMaintenanceIntensity intensity) &&
                   Enum.IsDefined(intensity);
        }
    }
}

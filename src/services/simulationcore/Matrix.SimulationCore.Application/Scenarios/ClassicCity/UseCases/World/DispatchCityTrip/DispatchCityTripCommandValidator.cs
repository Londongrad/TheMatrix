using FluentValidation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip
{
    public sealed class DispatchCityTripCommandValidator : AbstractValidator<DispatchCityTripCommand>
    {
        public DispatchCityTripCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
            RuleFor(x => x.FromId)
               .NotEmpty();
            RuleFor(x => x.ToId)
               .NotEmpty();
            RuleFor(x => x.FromKind)
               .Must(value => CityRouteMapPointKinds.IsSupported(CityRouteMapPointKinds.Normalize(value)))
               .WithMessage("Unsupported origin point kind.");
            RuleFor(x => x.ToKind)
               .Must(value => CityRouteMapPointKinds.IsSupported(CityRouteMapPointKinds.Normalize(value)))
               .WithMessage("Unsupported destination point kind.");
            RuleFor(x => x.Profile)
               .Must(value => CityRouteProfiles.IsSupported(CityRouteProfiles.Normalize(value)))
               .WithMessage("Unsupported trip movement profile.");
            RuleFor(x => x.Purpose)
               .Must(CityTripPurposeNames.IsSupported)
               .WithMessage("Unsupported trip purpose.");
            RuleFor(x => x.MovementCapabilityIndex)
               .InclusiveBetween(
                    from: CityActiveTrip.MovementCapabilityIndexMin,
                    to: CityActiveTrip.MovementCapabilityIndexMax);
            RuleFor(x => x.Subject)
               .MaximumLength(CityActiveTrip.MaxSubjectLength)
               .When(x => !string.IsNullOrWhiteSpace(x.Subject));
        }
    }
}

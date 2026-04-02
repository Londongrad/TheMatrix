using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public sealed class ResolveCityRouteQueryValidator : AbstractValidator<ResolveCityRouteQuery>
    {
        public ResolveCityRouteQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
            RuleFor(x => x.FromId)
               .NotEmpty();
            RuleFor(x => x.ToId)
               .NotEmpty();
            RuleFor(x => x.FromKind)
               .Must(value => CityRouteMapPointKinds.IsSupported(CityRouteMapPointKinds.Normalize(value)))
               .WithMessage("Route source kind is not supported.");
            RuleFor(x => x.ToKind)
               .Must(value => CityRouteMapPointKinds.IsSupported(CityRouteMapPointKinds.Normalize(value)))
               .WithMessage("Route destination kind is not supported.");
            RuleFor(x => x.Profile)
               .Must(value => CityRouteProfiles.IsSupported(CityRouteProfiles.Normalize(value)))
               .WithMessage("Route profile is not supported.");
        }
    }
}

using FluentValidation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoutesBatch
{
    public sealed class ResolveCityRoutesBatchQueryValidator : AbstractValidator<ResolveCityRoutesBatchQuery>
    {
        public ResolveCityRoutesBatchQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
            RuleFor(x => x.Routes)
               .NotNull()
               .Must(x => x is not null && x.Count > 0)
               .WithMessage("Routes batch must contain at least one route.")
               .Must(x => x is not null && x.Count <= 512)
               .WithMessage("Routes batch cannot contain more than 512 routes.");
            RuleForEach(x => x.Routes)
               .ChildRules(route =>
                {
                    route.RuleFor(x => x.Index)
                       .GreaterThanOrEqualTo(0);
                    route.RuleFor(x => x.FromId)
                       .NotEmpty();
                    route.RuleFor(x => x.ToId)
                       .NotEmpty();
                    route.RuleFor(x => x.FromKind)
                       .Must(value => CityRouteMapPointKinds.IsSupported(CityRouteMapPointKinds.Normalize(value)))
                       .WithMessage("Route source kind is not supported.");
                    route.RuleFor(x => x.ToKind)
                       .Must(value => CityRouteMapPointKinds.IsSupported(CityRouteMapPointKinds.Normalize(value)))
                       .WithMessage("Route destination kind is not supported.");
                    route.RuleFor(x => x.Profile)
                       .Must(value => CityRouteProfiles.IsSupported(CityRouteProfiles.Normalize(value)))
                       .WithMessage("Route profile is not supported.");
                });
        }
    }
}

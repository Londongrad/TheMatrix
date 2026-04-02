using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors
{
    public sealed class GetCityAnchorsQueryValidator : AbstractValidator<GetCityAnchorsQuery>
    {
        public GetCityAnchorsQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}

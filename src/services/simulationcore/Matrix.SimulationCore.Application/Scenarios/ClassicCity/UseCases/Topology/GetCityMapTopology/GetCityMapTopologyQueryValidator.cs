using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed class GetCityMapTopologyQueryValidator : AbstractValidator<GetCityMapTopologyQuery>
    {
        public GetCityMapTopologyQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}

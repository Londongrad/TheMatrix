using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph
{
    public sealed class GetCityRoadGraphQueryValidator : AbstractValidator<GetCityRoadGraphQuery>
    {
        public GetCityRoadGraphQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}

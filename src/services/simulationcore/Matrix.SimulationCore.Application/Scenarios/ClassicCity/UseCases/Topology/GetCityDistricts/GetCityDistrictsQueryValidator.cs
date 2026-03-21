using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts
{
    public sealed class GetCityDistrictsQueryValidator : AbstractValidator<GetCityDistrictsQuery>
    {
        public GetCityDistrictsQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}

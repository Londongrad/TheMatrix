using FluentValidation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts
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

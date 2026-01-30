using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Topology.GetCityDistricts
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

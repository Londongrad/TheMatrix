using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity
{
    public sealed class GetCityQueryValidator : AbstractValidator<GetCityQuery>
    {
        public GetCityQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}

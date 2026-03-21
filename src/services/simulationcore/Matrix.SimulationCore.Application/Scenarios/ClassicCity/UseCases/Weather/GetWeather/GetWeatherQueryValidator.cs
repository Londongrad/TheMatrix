using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather
{
    public sealed class GetWeatherQueryValidator : AbstractValidator<GetWeatherQuery>
    {
        public GetWeatherQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}

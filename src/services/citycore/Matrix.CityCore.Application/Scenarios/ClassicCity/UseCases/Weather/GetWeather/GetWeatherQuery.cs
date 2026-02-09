using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather
{
    public sealed record GetWeatherQuery(Guid CityId) : IRequest<CityWeatherDto?>;
}

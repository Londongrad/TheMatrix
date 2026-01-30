using MediatR;

namespace Matrix.CityCore.Application.UseCases.Weather.GetWeather
{
    public sealed record GetWeatherQuery(Guid CityId) : IRequest<CityWeatherDto?>;
}

using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather
{
    public sealed class GetWeatherQueryHandler(ICityWeatherRepository repository)
        : IRequestHandler<GetWeatherQuery, CityWeatherDto?>
    {
        public async Task<CityWeatherDto?> Handle(
            GetWeatherQuery request,
            CancellationToken cancellationToken)
        {
            CityWeather? weather = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            return weather is null
                ? null
                : CityWeatherDto.FromDomain(weather);
        }
    }
}

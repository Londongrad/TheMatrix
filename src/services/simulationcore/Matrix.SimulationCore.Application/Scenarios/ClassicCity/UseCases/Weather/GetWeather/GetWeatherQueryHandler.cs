using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather
{
    public sealed class GetWeatherQueryHandler(
        ICityWeatherRepository repository,
        ICityRepository cityRepository)
        : IRequestHandler<GetWeatherQuery, CityWeatherDto?>
    {
        public async Task<CityWeatherDto?> Handle(
            GetWeatherQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);
            City? city = await cityRepository.GetByIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (city is null)
                return null;

            CityWeather? weather = await repository.GetByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return weather is null
                ? null
                : CityWeatherDto.FromDomain(
                    weather: weather,
                    city: city);
        }
    }
}

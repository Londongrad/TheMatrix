using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather.GetWeather
{
    public sealed class GetWeatherQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsNull()
        {
            var cityId = Guid.NewGuid();
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var handler = new GetWeatherQueryHandler(
                repository: weatherRepository,
                cityRepository: cityRepository);

            CityWeatherDto? result = await handler.Handle(
                request: new GetWeatherQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: cityId,
                actual: cityRepository.RequestedCityId!.Value.Value);
            Assert.Null(weatherRepository.RequestedCityId);
        }

        [Fact]
        public async Task Handle_WhenCityExistsButWeatherDoesNotExist_ReturnsNull()
        {
            City city = ClassicCityTestSupport.CreateCity();
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var handler = new GetWeatherQueryHandler(
                repository: weatherRepository,
                cityRepository: cityRepository);

            CityWeatherDto? result = await handler.Handle(
                request: new GetWeatherQuery(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: city.Id.Value,
                actual: weatherRepository.RequestedCityId!.Value.Value);
        }

        [Fact]
        public async Task Handle_WhenWeatherExists_ReturnsMappedDto()
        {
            City city = ClassicCityTestSupport.CreateCity();
            CityWeather weather = WeatherTestSupport.CreateCityWeather(city.Id);
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository
            {
                WeatherByCityId = weather
            };
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var handler = new GetWeatherQueryHandler(
                repository: weatherRepository,
                cityRepository: cityRepository);

            CityWeatherDto? result = await handler.Handle(
                request: new GetWeatherQuery(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: city.Id.Value,
                actual: result!.CityId);
            Assert.Equal(
                expected: weather.ClimateProfile.ClimateZone.ToString(),
                actual: result.ClimateZone);
            Assert.Equal(
                expected: city.Environment.Hemisphere.ToString(),
                actual: result.Hemisphere);
            Assert.Equal(
                expected: city.Environment.UtcOffset.TotalMinutes,
                actual: result.UtcOffsetMinutes);
            Assert.Equal(
                expected: weather.CurrentState.Type.ToString(),
                actual: result.CurrentType);
            Assert.Equal(
                expected: weather.CurrentState.Severity.ToString(),
                actual: result.Severity);
            Assert.Equal(
                expected: weather.CurrentState.PrecipitationKind.ToString(),
                actual: result.PrecipitationKind);
            Assert.Equal(
                expected: weather.CurrentState.Temperature.Value,
                actual: result.TemperatureC);
            Assert.Equal(
                expected: weather.LastEvaluatedAt.ValueUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Null(result.ActiveOverride);
        }
    }
}

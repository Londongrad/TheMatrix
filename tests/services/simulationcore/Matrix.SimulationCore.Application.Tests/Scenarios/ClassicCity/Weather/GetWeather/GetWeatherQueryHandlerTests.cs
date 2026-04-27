using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather.GetWeather;

public sealed class GetWeatherQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsNull()
    {
        Guid cityId = Guid.NewGuid();
        var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
        var cityRepository = new Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ClassicCityTestSupport.FakeCityRepository();
        var handler = new GetWeatherQueryHandler(weatherRepository, cityRepository);

        var result = await handler.Handle(new GetWeatherQuery(cityId), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(cityId, cityRepository.RequestedCityId!.Value.Value);
        Assert.Null(weatherRepository.RequestedCityId);
    }

    [Fact]
    public async Task Handle_WhenCityExistsButWeatherDoesNotExist_ReturnsNull()
    {
        var city = Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ClassicCityTestSupport.CreateCity();
        var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
        var cityRepository = new Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var handler = new GetWeatherQueryHandler(weatherRepository, cityRepository);

        var result = await handler.Handle(new GetWeatherQuery(city.Id.Value), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(city.Id.Value, weatherRepository.RequestedCityId!.Value.Value);
    }

    [Fact]
    public async Task Handle_WhenWeatherExists_ReturnsMappedDto()
    {
        var city = Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ClassicCityTestSupport.CreateCity();
        var weather = WeatherTestSupport.CreateCityWeather(city.Id);
        var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository
        {
            WeatherByCityId = weather
        };
        var cityRepository = new Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var handler = new GetWeatherQueryHandler(weatherRepository, cityRepository);

        var result = await handler.Handle(new GetWeatherQuery(city.Id.Value), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(city.Id.Value, result!.CityId);
        Assert.Equal(weather.ClimateProfile.ClimateZone.ToString(), result.ClimateZone);
        Assert.Equal(city.Environment.Hemisphere.ToString(), result.Hemisphere);
        Assert.Equal(city.Environment.UtcOffset.TotalMinutes, result.UtcOffsetMinutes);
        Assert.Equal(weather.CurrentState.Type.ToString(), result.CurrentType);
        Assert.Equal(weather.CurrentState.Severity.ToString(), result.Severity);
        Assert.Equal(weather.CurrentState.PrecipitationKind.ToString(), result.PrecipitationKind);
        Assert.Equal(weather.CurrentState.Temperature.Value, result.TemperatureC);
        Assert.Equal(weather.LastEvaluatedAt.ValueUtc, result.LastEvaluatedAtUtc);
        Assert.Null(result.ActiveOverride);
    }
}

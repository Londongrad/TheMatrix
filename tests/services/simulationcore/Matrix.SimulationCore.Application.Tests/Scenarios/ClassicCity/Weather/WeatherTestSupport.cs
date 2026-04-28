using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;

internal static class WeatherTestSupport
{
    internal static readonly DateTimeOffset SimTimeUtc = new(2048, 4, 5, 6, 7, 8, TimeSpan.Zero);

    internal static CityWeather CreateCityWeather(CityId cityId)
    {
        return CityWeather.Create(
            cityId,
            CreateClimateProfile(),
            CreateWeatherState(),
            SimTime.FromUtc(SimTimeUtc));
    }

    internal static WeatherClimateProfile CreateClimateProfile()
    {
        return WeatherClimateProfile.Create(
            climateZone: ClimateZone.Temperate,
            temperatureProfile: SeasonalTemperatureProfile.Create(
                springAverage: TemperatureC.From(12m),
                summerAverage: TemperatureC.From(24m),
                autumnAverage: TemperatureC.From(10m),
                winterAverage: TemperatureC.From(-8m),
                dailySwing: TemperatureC.From(6m)),
            precipitationProfile: SeasonalPrecipitationProfile.Create(
                springHumidity: HumidityPercent.From(55m),
                summerHumidity: HumidityPercent.From(50m),
                autumnHumidity: HumidityPercent.From(65m),
                winterHumidity: HumidityPercent.From(70m),
                springDominantKind: PrecipitationKind.Rain,
                summerDominantKind: PrecipitationKind.Rain,
                autumnDominantKind: PrecipitationKind.Rain,
                winterDominantKind: PrecipitationKind.Snow),
            windProfile: SeasonalWindProfile.Create(
                springAverage: WindSpeedKph.From(16m),
                summerAverage: WindSpeedKph.From(12m),
                autumnAverage: WindSpeedKph.From(18m),
                winterAverage: WindSpeedKph.From(20m),
                gustHeadroom: WindSpeedKph.From(14m)),
            volatility: WeatherVolatility.From(0.35m),
            extremeWeatherProfile: ExtremeWeatherProfile.Create(
                maxOverallSeverity: WeatherSeverity.Severe,
                supportsThunderstorms: true,
                supportsSnowstorms: true,
                supportsFog: true,
                supportsHeatwaves: true));
    }

    internal static WeatherState CreateWeatherState()
    {
        var startedAt = SimTime.FromUtc(SimTimeUtc);

        return WeatherState.Create(
            type: WeatherType.Clear,
            severity: WeatherSeverity.Calm,
            precipitationKind: PrecipitationKind.None,
            temperature: TemperatureC.From(18m),
            humidity: HumidityPercent.From(48m),
            windSpeed: WindSpeedKph.From(9m),
            cloudCoverage: CloudCoveragePercent.From(12m),
            pressure: PressureHpa.From(1017m),
            startedAt: startedAt,
            expectedUntil: startedAt.Add(TimeSpan.FromHours(3)));
    }

    internal sealed class FakeCityWeatherRepository : ICityWeatherRepository
    {
        public CityWeather? WeatherByCityId { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public CityWeather? AddedWeather { get; private set; }

        public Task AddAsync(CityWeather cityWeather, CancellationToken cancellationToken)
        {
            AddedWeather = cityWeather;
            return Task.CompletedTask;
        }

        public Task<CityWeather?> GetByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(WeatherByCityId);
        }
    }
}

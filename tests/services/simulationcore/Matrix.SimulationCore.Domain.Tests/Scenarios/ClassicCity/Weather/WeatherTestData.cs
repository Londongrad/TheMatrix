using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

internal static class WeatherTestData
{
    internal static readonly CityId CityId = new(
        Guid.Parse("11111111-2222-3333-4444-555555555555"));
    internal static readonly SimTime StartedAt = SimTime.FromUtc(
        new DateTimeOffset(2042, 3, 1, 6, 0, 0, TimeSpan.Zero));
    internal static readonly SimTime ExpectedUntil = StartedAt.Add(TimeSpan.FromHours(3));
    internal static readonly SimTime Midpoint = StartedAt.Add(TimeSpan.FromHours(1));
    internal static readonly SimTime AfterEnd = ExpectedUntil.Add(TimeSpan.FromMinutes(1));
    internal static readonly SimTime NearEnd = ExpectedUntil.Add(TimeSpan.FromMinutes(-1));

    internal static WeatherState CreateWeatherState(
        WeatherType type = WeatherType.Clear,
        PrecipitationKind precipitationKind = PrecipitationKind.None,
        WeatherSeverity severity = WeatherSeverity.Calm,
        TemperatureC? temperature = null,
        HumidityPercent? humidity = null,
        WindSpeedKph? windSpeed = null,
        CloudCoveragePercent? cloudCoverage = null,
        PressureHpa? pressure = null,
        SimTime? startedAt = null,
        SimTime? expectedUntil = null)
    {
        return WeatherState.Create(
            type: type,
            severity: severity,
            precipitationKind: precipitationKind,
            temperature: temperature ?? TemperatureC.From(18m),
            humidity: humidity ?? HumidityPercent.From(45m),
            windSpeed: windSpeed ?? WindSpeedKph.From(12m),
            cloudCoverage: cloudCoverage ?? CloudCoveragePercent.From(10m),
            pressure: pressure ?? PressureHpa.From(1013m),
            startedAt: startedAt ?? StartedAt,
            expectedUntil: expectedUntil ?? ExpectedUntil);
    }

    internal static SeasonalTemperatureProfile CreateTemperatureProfile()
    {
        return SeasonalTemperatureProfile.Create(
            springAverage: TemperatureC.From(12m),
            summerAverage: TemperatureC.From(24m),
            autumnAverage: TemperatureC.From(10m),
            winterAverage: TemperatureC.From(-6m),
            dailySwing: TemperatureC.From(7m));
    }

    internal static SeasonalPrecipitationProfile CreatePrecipitationProfile()
    {
        return SeasonalPrecipitationProfile.Create(
            springHumidity: HumidityPercent.From(58m),
            summerHumidity: HumidityPercent.From(62m),
            autumnHumidity: HumidityPercent.From(70m),
            winterHumidity: HumidityPercent.From(77m),
            springDominantKind: PrecipitationKind.Rain,
            summerDominantKind: PrecipitationKind.Rain,
            autumnDominantKind: PrecipitationKind.Drizzle,
            winterDominantKind: PrecipitationKind.Snow);
    }

    internal static SeasonalWindProfile CreateWindProfile()
    {
        return SeasonalWindProfile.Create(
            springAverage: WindSpeedKph.From(16m),
            summerAverage: WindSpeedKph.From(12m),
            autumnAverage: WindSpeedKph.From(19m),
            winterAverage: WindSpeedKph.From(23m),
            gustHeadroom: WindSpeedKph.From(31m));
    }

    internal static ExtremeWeatherProfile CreateExtremeWeatherProfile()
    {
        return ExtremeWeatherProfile.Create(
            maxOverallSeverity: WeatherSeverity.Extreme,
            supportsThunderstorms: true,
            supportsSnowstorms: true,
            supportsFog: true,
            supportsHeatwaves: true);
    }

    internal static WeatherClimateProfile CreateClimateProfile()
    {
        return WeatherClimateProfile.Create(
            climateZone: ClimateZone.Temperate,
            temperatureProfile: CreateTemperatureProfile(),
            precipitationProfile: CreatePrecipitationProfile(),
            windProfile: CreateWindProfile(),
            volatility: WeatherVolatility.From(0.25m),
            extremeWeatherProfile: CreateExtremeWeatherProfile());
    }

    internal static WeatherClimateProfile CreateAlternativeClimateProfile()
    {
        return WeatherClimateProfile.Create(
            climateZone: ClimateZone.Continental,
            temperatureProfile: SeasonalTemperatureProfile.Create(
                springAverage: TemperatureC.From(9m),
                summerAverage: TemperatureC.From(28m),
                autumnAverage: TemperatureC.From(7m),
                winterAverage: TemperatureC.From(-14m),
                dailySwing: TemperatureC.From(11m)),
            precipitationProfile: SeasonalPrecipitationProfile.Create(
                springHumidity: HumidityPercent.From(52m),
                summerHumidity: HumidityPercent.From(55m),
                autumnHumidity: HumidityPercent.From(63m),
                winterHumidity: HumidityPercent.From(71m),
                springDominantKind: PrecipitationKind.Rain,
                summerDominantKind: PrecipitationKind.None,
                autumnDominantKind: PrecipitationKind.Drizzle,
                winterDominantKind: PrecipitationKind.Snow),
            windProfile: SeasonalWindProfile.Create(
                springAverage: WindSpeedKph.From(14m),
                summerAverage: WindSpeedKph.From(10m),
                autumnAverage: WindSpeedKph.From(18m),
                winterAverage: WindSpeedKph.From(24m),
                gustHeadroom: WindSpeedKph.From(35m)),
            volatility: WeatherVolatility.From(0.4m),
            extremeWeatherProfile: ExtremeWeatherProfile.Create(
                maxOverallSeverity: WeatherSeverity.Severe,
                supportsThunderstorms: true,
                supportsSnowstorms: true,
                supportsFog: false,
                supportsHeatwaves: false));
    }

    internal static CityWeather CreateCityWeather(
        WeatherState? currentState = null,
        WeatherClimateProfile? climateProfile = null,
        SimTime? createdAt = null)
    {
        return CityWeather.Create(
            cityId: CityId,
            climateProfile: climateProfile ?? CreateClimateProfile(),
            currentState: currentState ?? CreateWeatherState(startedAt: StartedAt, expectedUntil: ExpectedUntil),
            createdAt: createdAt ?? Midpoint);
    }
}

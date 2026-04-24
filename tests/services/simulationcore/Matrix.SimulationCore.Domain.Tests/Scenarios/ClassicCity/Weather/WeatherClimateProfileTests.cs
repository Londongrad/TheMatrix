using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

public sealed class WeatherClimateProfileTests
{
    private const string InvalidClimateProfileCode = "SimulationCore.Weather.ClimateProfile.Invalid";
    private const string InvalidEnumCode = "Domain.Guard.InvalidEnum";

    [Fact]
    public void SeasonalTemperatureProfile_Create_WithNegativeDailySwing_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SeasonalTemperatureProfile.Create(
            springAverage: TemperatureC.From(12m),
            summerAverage: TemperatureC.From(24m),
            autumnAverage: TemperatureC.From(10m),
            winterAverage: TemperatureC.From(-6m),
            dailySwing: TemperatureC.From(-1m)));

        Assert.Equal(InvalidClimateProfileCode, exception.Code);
        Assert.Equal("dailySwing", exception.PropertyName);
    }

    [Fact]
    public void SeasonalTemperatureProfile_GetAverage_ReturnsSeasonalValues_AndRejectsInvalidSeason()
    {
        var profile = WeatherTestData.CreateTemperatureProfile();

        Assert.Equal(profile.SpringAverage, profile.GetAverage(WeatherSeason.Spring));
        Assert.Equal(profile.SummerAverage, profile.GetAverage(WeatherSeason.Summer));
        Assert.Equal(profile.AutumnAverage, profile.GetAverage(WeatherSeason.Autumn));
        Assert.Equal(profile.WinterAverage, profile.GetAverage(WeatherSeason.Winter));

        var exception = Assert.Throws<DomainException>(() => profile.GetAverage((WeatherSeason)999));

        Assert.Equal(InvalidClimateProfileCode, exception.Code);
        Assert.Equal("season", exception.PropertyName);
    }

    [Fact]
    public void SeasonalPrecipitationProfile_Create_WithInvalidKind_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SeasonalPrecipitationProfile.Create(
            springHumidity: HumidityPercent.From(58m),
            summerHumidity: HumidityPercent.From(62m),
            autumnHumidity: HumidityPercent.From(70m),
            winterHumidity: HumidityPercent.From(77m),
            springDominantKind: (PrecipitationKind)999,
            summerDominantKind: PrecipitationKind.Rain,
            autumnDominantKind: PrecipitationKind.Drizzle,
            winterDominantKind: PrecipitationKind.Snow));

        Assert.Equal(InvalidEnumCode, exception.Code);
        Assert.Equal("SpringDominantKind", exception.PropertyName);
    }

    [Fact]
    public void SeasonalPrecipitationProfile_GetMembers_ReturnSeasonalValues_AndRejectInvalidSeason()
    {
        var profile = WeatherTestData.CreatePrecipitationProfile();

        Assert.Equal(profile.SpringHumidity, profile.GetHumidity(WeatherSeason.Spring));
        Assert.Equal(profile.WinterHumidity, profile.GetHumidity(WeatherSeason.Winter));
        Assert.Equal(profile.SummerDominantKind, profile.GetDominantKind(WeatherSeason.Summer));
        Assert.Equal(profile.AutumnDominantKind, profile.GetDominantKind(WeatherSeason.Autumn));

        var humidityException = Assert.Throws<DomainException>(() => profile.GetHumidity((WeatherSeason)999));
        var kindException = Assert.Throws<DomainException>(() => profile.GetDominantKind((WeatherSeason)999));

        Assert.Equal(InvalidClimateProfileCode, humidityException.Code);
        Assert.Equal("season", humidityException.PropertyName);
        Assert.Equal(InvalidClimateProfileCode, kindException.Code);
        Assert.Equal("season", kindException.PropertyName);
    }

    [Fact]
    public void SeasonalWindProfile_GetAverage_ReturnsSeasonalValues_AndRejectsInvalidSeason()
    {
        var profile = WeatherTestData.CreateWindProfile();

        Assert.Equal(profile.SpringAverage, profile.GetAverage(WeatherSeason.Spring));
        Assert.Equal(profile.WinterAverage, profile.GetAverage(WeatherSeason.Winter));

        var exception = Assert.Throws<DomainException>(() => profile.GetAverage((WeatherSeason)999));

        Assert.Equal(InvalidClimateProfileCode, exception.Code);
        Assert.Equal("season", exception.PropertyName);
    }

    [Fact]
    public void ExtremeWeatherProfile_Create_WithCalmSeverityAndCapabilities_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => ExtremeWeatherProfile.Create(
            maxOverallSeverity: WeatherSeverity.Calm,
            supportsThunderstorms: true,
            supportsSnowstorms: false,
            supportsFog: false,
            supportsHeatwaves: false));

        Assert.Equal(InvalidClimateProfileCode, exception.Code);
        Assert.Equal("maxOverallSeverity", exception.PropertyName);
    }

    [Fact]
    public void ExtremeWeatherProfile_Create_WithInvalidSeverity_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => ExtremeWeatherProfile.Create(
            maxOverallSeverity: (WeatherSeverity)999,
            supportsThunderstorms: false,
            supportsSnowstorms: false,
            supportsFog: false,
            supportsHeatwaves: false));

        Assert.Equal(InvalidEnumCode, exception.Code);
        Assert.Equal("MaxOverallSeverity", exception.PropertyName);
    }

    [Fact]
    public void WeatherClimateProfile_Create_WithValidProfiles_CreatesClimateProfile_AndReturnsBaselines()
    {
        var temperatureProfile = WeatherTestData.CreateTemperatureProfile();
        var precipitationProfile = WeatherTestData.CreatePrecipitationProfile();
        var windProfile = WeatherTestData.CreateWindProfile();
        var volatility = WeatherVolatility.From(0.25m);
        var extremeWeatherProfile = WeatherTestData.CreateExtremeWeatherProfile();

        var profile = WeatherClimateProfile.Create(
            climateZone: ClimateZone.Temperate,
            temperatureProfile: temperatureProfile,
            precipitationProfile: precipitationProfile,
            windProfile: windProfile,
            volatility: volatility,
            extremeWeatherProfile: extremeWeatherProfile);

        Assert.Equal(ClimateZone.Temperate, profile.ClimateZone);
        Assert.Equal(temperatureProfile, profile.TemperatureProfile);
        Assert.Equal(precipitationProfile, profile.PrecipitationProfile);
        Assert.Equal(windProfile, profile.WindProfile);
        Assert.Equal(volatility, profile.Volatility);
        Assert.Equal(extremeWeatherProfile, profile.ExtremeWeatherProfile);
        Assert.Equal(temperatureProfile.GetAverage(WeatherSeason.Summer), profile.GetBaselineTemperature(WeatherSeason.Summer));
        Assert.Equal(precipitationProfile.GetHumidity(WeatherSeason.Winter), profile.GetBaselineHumidity(WeatherSeason.Winter));
        Assert.Equal(precipitationProfile.GetDominantKind(WeatherSeason.Spring), profile.GetDominantPrecipitation(WeatherSeason.Spring));
        Assert.Equal(windProfile.GetAverage(WeatherSeason.Autumn), profile.GetBaselineWindSpeed(WeatherSeason.Autumn));
    }

    [Fact]
    public void WeatherClimateProfile_Create_WithInvalidClimateZone_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WeatherClimateProfile.Create(
            climateZone: (ClimateZone)999,
            temperatureProfile: WeatherTestData.CreateTemperatureProfile(),
            precipitationProfile: WeatherTestData.CreatePrecipitationProfile(),
            windProfile: WeatherTestData.CreateWindProfile(),
            volatility: WeatherVolatility.From(0.25m),
            extremeWeatherProfile: WeatherTestData.CreateExtremeWeatherProfile()));

        Assert.Equal(InvalidEnumCode, exception.Code);
        Assert.Equal("ClimateZone", exception.PropertyName);
    }

    [Fact]
    public void WeatherClimateProfile_Create_WithMissingTemperatureProfile_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WeatherClimateProfile.Create(
            climateZone: ClimateZone.Temperate,
            temperatureProfile: null!,
            precipitationProfile: WeatherTestData.CreatePrecipitationProfile(),
            windProfile: WeatherTestData.CreateWindProfile(),
            volatility: WeatherVolatility.From(0.25m),
            extremeWeatherProfile: WeatherTestData.CreateExtremeWeatherProfile()));

        Assert.Equal(InvalidClimateProfileCode, exception.Code);
        Assert.Equal("temperatureProfile", exception.PropertyName);
    }
}

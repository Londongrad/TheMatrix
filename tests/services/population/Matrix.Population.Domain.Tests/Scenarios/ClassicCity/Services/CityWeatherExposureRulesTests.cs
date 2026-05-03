using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityWeatherExposureRulesTests
{
    [Fact]
    public void IsAdverseExposureWeather_WhenModerateStorm_ReturnsTrue()
    {
        bool adverse = CityWeatherExposureRules.IsAdverseExposureWeather(
            CreateWeather(
                type: PopulationWeatherType.Storm,
                severity: PopulationWeatherSeverity.Moderate,
                temperatureC: 16m,
                windSpeedKph: 55m));

        Assert.True(adverse);
    }

    [Fact]
    public void IsAdverseExposureWeather_WhenMildHeatwave_ReturnsFalse()
    {
        bool adverse = CityWeatherExposureRules.IsAdverseExposureWeather(
            CreateWeather(
                type: PopulationWeatherType.Heatwave,
                severity: PopulationWeatherSeverity.Mild,
                temperatureC: 31m));

        Assert.False(adverse);
    }

    [Fact]
    public void IsRecoveryWeather_WhenOvercastAndStable_ReturnsTrue()
    {
        bool recovery = CityWeatherExposureRules.IsRecoveryWeather(
            CreateWeather(
                type: PopulationWeatherType.Overcast,
                severity: PopulationWeatherSeverity.Mild,
                temperatureC: 20m,
                humidityPercent: 55m,
                windSpeedKph: 12m));

        Assert.True(recovery);
    }

    [Fact]
    public void IsRecoveryWeather_WhenConditionsAreWindyOrWet_ReturnsFalse()
    {
        bool recovery = CityWeatherExposureRules.IsRecoveryWeather(
            CreateWeather(
                type: PopulationWeatherType.Clear,
                severity: PopulationWeatherSeverity.Calm,
                temperatureC: 20m,
                precipitationKind: PopulationPrecipitationKind.Rain,
                windSpeedKph: 30m));

        Assert.False(recovery);
    }

    [Fact]
    public void IsComfortableRecoveryWeather_WhenClearAndComfortable_ReturnsTrue()
    {
        bool comfortable = CityWeatherExposureRules.IsComfortableRecoveryWeather(
            CreateWeather(
                type: PopulationWeatherType.Clear,
                severity: PopulationWeatherSeverity.Calm,
                temperatureC: 21m,
                humidityPercent: 50m,
                windSpeedKph: 10m));

        Assert.True(comfortable);
    }

    [Fact]
    public void IsComfortableRecoveryWeather_WhenHumidityIsOutsideComfortBand_ReturnsFalse()
    {
        bool comfortable = CityWeatherExposureRules.IsComfortableRecoveryWeather(
            CreateWeather(
                type: PopulationWeatherType.Clear,
                severity: PopulationWeatherSeverity.Calm,
                temperatureC: 21m,
                humidityPercent: 80m,
                windSpeedKph: 10m));

        Assert.False(comfortable);
    }

    private static WeatherImpactProfile CreateWeather(
        PopulationWeatherType type,
        PopulationWeatherSeverity severity,
        decimal temperatureC,
        decimal humidityPercent = 45m,
        decimal windSpeedKph = 12m,
        PopulationPrecipitationKind precipitationKind = PopulationPrecipitationKind.None)
    {
        return new WeatherImpactProfile(
            Type: type,
            Severity: severity,
            PrecipitationKind: precipitationKind,
            TemperatureC: temperatureC,
            HumidityPercent: humidityPercent,
            WindSpeedKph: windSpeedKph,
            CloudCoveragePercent: 35m,
            PressureHpa: 1012m);
    }
}

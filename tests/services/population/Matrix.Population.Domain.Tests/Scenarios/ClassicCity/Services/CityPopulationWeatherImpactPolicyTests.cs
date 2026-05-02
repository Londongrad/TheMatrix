using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityPopulationWeatherImpactPolicyTests
{
    [Fact]
    public void CalculateDifferential_WhenPersonIsDead_ReturnsNone()
    {
        var policy = new CityPopulationWeatherImpactPolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson();
        deceasedResident.Die(new DateOnly(2048, 7, 10));

        Matrix.Population.Domain.Models.PersonWeatherImpact impact = policy.CalculateDifferential(
            person: deceasedResident,
            currentDate: new DateOnly(2048, 7, 10),
            previousWeather: CreateWeather(
                type: PopulationWeatherType.Clear,
                severity: PopulationWeatherSeverity.Calm,
                temperatureC: 22m),
            currentWeather: CreateWeather(
                type: PopulationWeatherType.Storm,
                severity: PopulationWeatherSeverity.Severe,
                temperatureC: 18m,
                windSpeedKph: 70m),
            environment: null);

        Assert.Equal(Matrix.Population.Domain.Models.PersonWeatherImpact.None, impact);
    }

    [Fact]
    public void CalculateDifferential_WhenStormAbruptlyStartsForSenior_ReturnsExpectedAdverseDelta()
    {
        var policy = new CityPopulationWeatherImpactPolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person senior = PopulationTestData.CreateAdultPerson(
            birthDate: new DateOnly(1960, 7, 10),
            currentDate: new DateOnly(2048, 7, 10));

        Matrix.Population.Domain.Models.PersonWeatherImpact impact = policy.CalculateDifferential(
            person: senior,
            currentDate: new DateOnly(2048, 7, 10),
            previousWeather: CreateWeather(
                type: PopulationWeatherType.Overcast,
                severity: PopulationWeatherSeverity.Mild,
                temperatureC: 18m),
            currentWeather: CreateWeather(
                type: PopulationWeatherType.Storm,
                severity: PopulationWeatherSeverity.Severe,
                temperatureC: 17m,
                windSpeedKph: 70m),
            environment: null);

        Assert.Equal(-4, impact.HealthDelta);
        Assert.Equal(-6, impact.HappinessDelta);
        Assert.True(impact.HasEffect);
    }

    [Fact]
    public void CalculateDifferential_WhenWeatherReliefOccurs_DoesNotHealHealthAndClampsPositiveHappiness()
    {
        var policy = new CityPopulationWeatherImpactPolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person adult = PopulationTestData.CreateAdultPerson(
            currentDate: new DateOnly(2048, 1, 10));

        Matrix.Population.Domain.Models.PersonWeatherImpact impact = policy.CalculateDifferential(
            person: adult,
            currentDate: new DateOnly(2048, 1, 10),
            previousWeather: CreateWeather(
                type: PopulationWeatherType.ColdSnap,
                severity: PopulationWeatherSeverity.Extreme,
                temperatureC: -20m),
            currentWeather: CreateWeather(
                type: PopulationWeatherType.Clear,
                severity: PopulationWeatherSeverity.Calm,
                temperatureC: 22m,
                humidityPercent: 50m,
                windSpeedKph: 8m),
            environment: null);

        Assert.Equal(0, impact.HealthDelta);
        Assert.Equal(4, impact.HappinessDelta);
    }

    [Fact]
    public void CalculateDifferential_WhenClimateAdaptationIsHigh_ReducesAdverseHeatwaveTransition()
    {
        var policy = new CityPopulationWeatherImpactPolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person adult = PopulationTestData.CreateAdultPerson(
            currentDate: new DateOnly(2048, 7, 10));
        WeatherImpactProfile previousWeather = CreateWeather(
            type: PopulationWeatherType.Overcast,
            severity: PopulationWeatherSeverity.Mild,
            temperatureC: 22m,
            humidityPercent: 50m,
            windSpeedKph: 8m);
        WeatherImpactProfile currentWeather = CreateWeather(
            type: PopulationWeatherType.Heatwave,
            severity: PopulationWeatherSeverity.Moderate,
            temperatureC: 33m);

        Matrix.Population.Domain.Models.PersonWeatherImpact baselineImpact = policy.CalculateDifferential(
            person: adult,
            currentDate: new DateOnly(2048, 7, 10),
            previousWeather: previousWeather,
            currentWeather: currentWeather,
            environment: null);
        Matrix.Population.Domain.Models.PersonWeatherImpact adaptedImpact = policy.CalculateDifferential(
            person: adult,
            currentDate: new DateOnly(2048, 7, 10),
            previousWeather: previousWeather,
            currentWeather: currentWeather,
            environment: CreateEnvironment(
                climateZone: PopulationClimateZone.Tropical,
                hemisphere: PopulationHemisphere.Northern));

        Assert.Equal(-2, baselineImpact.HealthDelta);
        Assert.Equal(-3, baselineImpact.HappinessDelta);
        Assert.Equal(0, adaptedImpact.HealthDelta);
        Assert.Equal(-1, adaptedImpact.HappinessDelta);
    }

    private static WeatherImpactProfile CreateWeather(
        PopulationWeatherType type,
        PopulationWeatherSeverity severity,
        decimal temperatureC,
        decimal humidityPercent = 45m,
        decimal windSpeedKph = 12m)
    {
        return new WeatherImpactProfile(
            Type: type,
            Severity: severity,
            PrecipitationKind: PopulationPrecipitationKind.None,
            TemperatureC: temperatureC,
            HumidityPercent: humidityPercent,
            WindSpeedKph: windSpeedKph,
            CloudCoveragePercent: 35m,
            PressureHpa: 1012m);
    }

    private static CityPopulationEnvironment CreateEnvironment(
        PopulationClimateZone climateZone,
        PopulationHemisphere hemisphere)
    {
        return CityPopulationEnvironment.Create(
            cityId: CityId.From(Guid.Parse("56565656-5656-5656-5656-565656565656")),
            climateZone: climateZone,
            hemisphere: hemisphere,
            utcOffsetMinutes: 0,
            createdAtUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero));
    }
}

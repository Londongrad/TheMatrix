using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityPopulationWeatherExposurePolicyTests
{
    [Fact]
    public void Calculate_WhenPersonIsDead_ReturnsNone()
    {
        var policy = new CityPopulationWeatherExposurePolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson();
        deceasedResident.Die(new DateOnly(2048, 7, 10));

        Matrix.Population.Domain.Models.PersonWeatherImpact impact = policy.Calculate(
            person: deceasedResident,
            currentDate: new DateOnly(2048, 7, 10),
            segment: CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m),
                effectStartedAtUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero),
                intervalStartUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero),
                intervalEndUtc: new DateTimeOffset(2048, 7, 10, 6, 0, 0, TimeSpan.Zero)),
            environment: null);

        Assert.Equal(Matrix.Population.Domain.Models.PersonWeatherImpact.None, impact);
    }

    [Fact]
    public void Calculate_WhenExtremeHeatwaveCompletesExposureBlockForSenior_ReturnsExpectedAdverseImpact()
    {
        var policy = new CityPopulationWeatherExposurePolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person senior = PopulationTestData.CreateAdultPerson(
            firstName: "Nikolay",
            lastName: "Ivanov",
            birthDate: new DateOnly(1960, 7, 10),
            currentDate: new DateOnly(2048, 7, 10));

        Matrix.Population.Domain.Models.PersonWeatherImpact impact = policy.Calculate(
            person: senior,
            currentDate: new DateOnly(2048, 7, 10),
            segment: CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m),
                effectStartedAtUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero),
                intervalStartUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero),
                intervalEndUtc: new DateTimeOffset(2048, 7, 10, 6, 0, 0, TimeSpan.Zero)),
            environment: null);

        Assert.Equal(-3, impact.HealthDelta);
        Assert.Equal(-3, impact.HappinessDelta);
        Assert.True(impact.HasEffect);
    }

    [Fact]
    public void Calculate_WhenClimateAdaptationIsHigh_ReducesAdverseHeatwaveImpact()
    {
        var policy = new CityPopulationWeatherExposurePolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person adult = PopulationTestData.CreateAdultPerson(
            currentDate: new DateOnly(2048, 7, 10));
        CityWeatherExposureSegment segment = CreateAdverseSegment(
            weather: CreateWeather(
                type: PopulationWeatherType.Heatwave,
                severity: PopulationWeatherSeverity.Severe,
                temperatureC: 40m),
            effectStartedAtUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero),
            intervalStartUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero),
            intervalEndUtc: new DateTimeOffset(2048, 7, 10, 18, 0, 0, TimeSpan.Zero));

        Matrix.Population.Domain.Models.PersonWeatherImpact baselineImpact = policy.Calculate(
            person: adult,
            currentDate: new DateOnly(2048, 7, 10),
            segment: segment,
            environment: null);
        Matrix.Population.Domain.Models.PersonWeatherImpact adaptedImpact = policy.Calculate(
            person: adult,
            currentDate: new DateOnly(2048, 7, 10),
            segment: segment,
            environment: CreateEnvironment(
                climateZone: PopulationClimateZone.Tropical,
                hemisphere: PopulationHemisphere.Northern));

        Assert.Equal(-1, baselineImpact.HealthDelta);
        Assert.Equal(-2, baselineImpact.HappinessDelta);
        Assert.Equal(0, adaptedImpact.HealthDelta);
        Assert.Equal(0, adaptedImpact.HappinessDelta);
    }

    [Fact]
    public void Calculate_WhenRecoveryBlockCompletesAfterExtremeHeatwave_ReturnsPositiveRelief()
    {
        var policy = new CityPopulationWeatherExposurePolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        Matrix.Population.Domain.Entities.Person adult = PopulationTestData.CreateAdultPerson(
            currentDate: new DateOnly(2048, 7, 11));

        Matrix.Population.Domain.Models.PersonWeatherImpact impact = policy.Calculate(
            person: adult,
            currentDate: new DateOnly(2048, 7, 11),
            segment: new CityWeatherExposureSegment(
                Kind: CityWeatherExposureKind.Recovery,
                Weather: CreateWeather(
                    type: PopulationWeatherType.Clear,
                    severity: PopulationWeatherSeverity.Mild,
                    temperatureC: 22m,
                    humidityPercent: 50m,
                    windSpeedKph: 10m),
                EffectStartedAtSimTimeUtc: new DateTimeOffset(2048, 7, 10, 18, 0, 0, TimeSpan.Zero),
                IntervalStartSimTimeUtc: new DateTimeOffset(2048, 7, 10, 18, 0, 0, TimeSpan.Zero),
                IntervalEndSimTimeUtc: new DateTimeOffset(2048, 7, 11, 6, 0, 0, TimeSpan.Zero),
                SourceWeather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m)),
            environment: null);

        Assert.Equal(1, impact.HealthDelta);
        Assert.Equal(3, impact.HappinessDelta);
        Assert.True(impact.HasEffect);
    }

    private static CityWeatherExposureSegment CreateAdverseSegment(
        WeatherImpactProfile weather,
        DateTimeOffset effectStartedAtUtc,
        DateTimeOffset intervalStartUtc,
        DateTimeOffset intervalEndUtc)
    {
        return new CityWeatherExposureSegment(
            Kind: CityWeatherExposureKind.Adverse,
            Weather: weather,
            EffectStartedAtSimTimeUtc: effectStartedAtUtc,
            IntervalStartSimTimeUtc: intervalStartUtc,
            IntervalEndSimTimeUtc: intervalEndUtc);
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
            cityId: CityId.From(Guid.Parse("34343434-3434-3434-3434-343434343434")),
            climateZone: climateZone,
            hemisphere: hemisphere,
            utcOffsetMinutes: 0,
            createdAtUtc: new DateTimeOffset(2048, 7, 10, 0, 0, 0, TimeSpan.Zero));
    }
}

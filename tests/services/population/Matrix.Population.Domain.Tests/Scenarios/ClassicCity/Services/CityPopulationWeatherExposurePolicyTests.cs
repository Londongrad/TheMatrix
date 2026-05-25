using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationWeatherExposurePolicyTests
    {
        [Fact]
        public void Calculate_WhenPersonIsDead_ReturnsNone()
        {
            var policy = new CityPopulationWeatherExposurePolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10));

            PersonWeatherImpact impact = policy.Calculate(
                person: deceasedResident,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
                segment: CreateAdverseSegment(
                    weather: CreateWeather(
                        type: PopulationWeatherType.Heatwave,
                        severity: PopulationWeatherSeverity.Extreme,
                        temperatureC: 39m),
                    effectStartedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    intervalStartUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    intervalEndUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 6,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                environment: null);

            Assert.Equal(
                expected: PersonWeatherImpact.None,
                actual: impact);
        }

        [Fact]
        public void Calculate_WhenExtremeHeatwaveCompletesExposureBlockForSenior_ReturnsExpectedAdverseImpact()
        {
            var policy = new CityPopulationWeatherExposurePolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person senior = PopulationTestData.CreateAdultPerson(
                firstName: "Nikolay",
                lastName: "Ivanov",
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10));

            PersonWeatherImpact impact = policy.Calculate(
                person: senior,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
                segment: CreateAdverseSegment(
                    weather: CreateWeather(
                        type: PopulationWeatherType.Heatwave,
                        severity: PopulationWeatherSeverity.Extreme,
                        temperatureC: 39m),
                    effectStartedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    intervalStartUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    intervalEndUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 6,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                environment: null);

            Assert.Equal(
                expected: -3,
                actual: impact.HealthDelta);
            Assert.Equal(
                expected: -3,
                actual: impact.HappinessDelta);
            Assert.True(impact.HasEffect);
        }

        [Fact]
        public void Calculate_WhenClimateAdaptationIsHigh_ReducesAdverseHeatwaveImpact()
        {
            var policy = new CityPopulationWeatherExposurePolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person adult = PopulationTestData.CreateAdultPerson(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10));
            CityWeatherExposureSegment segment = CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Severe,
                    temperatureC: 40m),
                effectStartedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 7,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                intervalStartUtc: new DateTimeOffset(
                    year: 2048,
                    month: 7,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                intervalEndUtc: new DateTimeOffset(
                    year: 2048,
                    month: 7,
                    day: 10,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            PersonWeatherImpact baselineImpact = policy.Calculate(
                person: adult,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
                segment: segment,
                environment: null);
            PersonWeatherImpact adaptedImpact = policy.Calculate(
                person: adult,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
                segment: segment,
                environment: CreateEnvironment(
                    climateZone: PopulationClimateZone.Tropical,
                    hemisphere: PopulationHemisphere.Northern));

            Assert.Equal(
                expected: -1,
                actual: baselineImpact.HealthDelta);
            Assert.Equal(
                expected: -2,
                actual: baselineImpact.HappinessDelta);
            Assert.Equal(
                expected: 0,
                actual: adaptedImpact.HealthDelta);
            Assert.Equal(
                expected: 0,
                actual: adaptedImpact.HappinessDelta);
        }

        [Fact]
        public void Calculate_WhenRecoveryBlockCompletesAfterExtremeHeatwave_ReturnsPositiveRelief()
        {
            var policy = new CityPopulationWeatherExposurePolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person adult = PopulationTestData.CreateAdultPerson(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 11));

            PersonWeatherImpact impact = policy.Calculate(
                person: adult,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 11),
                segment: new CityWeatherExposureSegment(
                    Kind: CityWeatherExposureKind.Recovery,
                    Weather: CreateWeather(
                        type: PopulationWeatherType.Clear,
                        severity: PopulationWeatherSeverity.Mild,
                        temperatureC: 22m,
                        humidityPercent: 50m,
                        windSpeedKph: 10m),
                    EffectStartedAtSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    IntervalStartSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 10,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    IntervalEndSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 11,
                        hour: 6,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    SourceWeather: CreateWeather(
                        type: PopulationWeatherType.Heatwave,
                        severity: PopulationWeatherSeverity.Extreme,
                        temperatureC: 39m)),
                environment: null);

            Assert.Equal(
                expected: 1,
                actual: impact.HealthDelta);
            Assert.Equal(
                expected: 3,
                actual: impact.HappinessDelta);
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
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 7,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}

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
    public sealed class CityPopulationWeatherImpactPolicyTests
    {
        [Fact]
        public void CalculateDifferential_WhenPersonIsDead_ReturnsNone()
        {
            var policy = new CityPopulationWeatherImpactPolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10));

            PersonWeatherImpact impact = policy.CalculateDifferential(
                person: deceasedResident,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
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

            Assert.Equal(
                expected: PersonWeatherImpact.None,
                actual: impact);
        }

        [Fact]
        public void CalculateDifferential_WhenStormAbruptlyStartsForSenior_ReturnsExpectedAdverseDelta()
        {
            var policy = new CityPopulationWeatherImpactPolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person senior = PopulationTestData.CreateAdultPerson(
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10));

            PersonWeatherImpact impact = policy.CalculateDifferential(
                person: senior,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
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

            Assert.Equal(
                expected: -4,
                actual: impact.HealthDelta);
            Assert.Equal(
                expected: -6,
                actual: impact.HappinessDelta);
            Assert.True(impact.HasEffect);
        }

        [Fact]
        public void CalculateDifferential_WhenWeatherReliefOccurs_DoesNotHealHealthAndClampsPositiveHappiness()
        {
            var policy = new CityPopulationWeatherImpactPolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person adult = PopulationTestData.CreateAdultPerson(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 1,
                    day: 10));

            PersonWeatherImpact impact = policy.CalculateDifferential(
                person: adult,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 1,
                    day: 10),
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

            Assert.Equal(
                expected: 0,
                actual: impact.HealthDelta);
            Assert.Equal(
                expected: 4,
                actual: impact.HappinessDelta);
        }

        [Fact]
        public void CalculateDifferential_WhenClimateAdaptationIsHigh_ReducesAdverseHeatwaveTransition()
        {
            var policy = new CityPopulationWeatherImpactPolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
            Person adult = PopulationTestData.CreateAdultPerson(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10));
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

            PersonWeatherImpact baselineImpact = policy.CalculateDifferential(
                person: adult,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
                previousWeather: previousWeather,
                currentWeather: currentWeather,
                environment: null);
            PersonWeatherImpact adaptedImpact = policy.CalculateDifferential(
                person: adult,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 7,
                    day: 10),
                previousWeather: previousWeather,
                currentWeather: currentWeather,
                environment: CreateEnvironment(
                    climateZone: PopulationClimateZone.Tropical,
                    hemisphere: PopulationHemisphere.Northern));

            Assert.Equal(
                expected: -2,
                actual: baselineImpact.HealthDelta);
            Assert.Equal(
                expected: -3,
                actual: baselineImpact.HappinessDelta);
            Assert.Equal(
                expected: 0,
                actual: adaptedImpact.HealthDelta);
            Assert.Equal(
                expected: -1,
                actual: adaptedImpact.HappinessDelta);
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

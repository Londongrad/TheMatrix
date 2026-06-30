using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class PendingWeatherHealthImpactStepTests
    {
        private static readonly DateOnly CurrentDate = new(2048, 7, 10);
        private static readonly DateTimeOffset OccurredAtUtc =
            new(2048, 7, 10, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void CalculateHealthDelta_UsesTransitionDateAndEnvironmentSnapshot()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(1960, 7, 10),
                currentDate: CurrentDate);
            var policy = new CityPopulationWeatherImpactPolicy(
                new CityPopulationClimateAdaptationPolicy());
            CityPopulationPendingWeatherImpact pendingImpact = CreatePendingImpact(
                impactId: Guid.NewGuid(),
                environment: CityPopulationEnvironment.Create(
                    cityId: CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                    climateZone: PopulationClimateZone.Tropical,
                    hemisphere: PopulationHemisphere.Northern,
                    utcOffsetMinutes: 0,
                    createdAtUtc: OccurredAtUtc));
            PersonWeatherImpact expected = policy.CalculateDifferential(
                person: resident,
                currentDate: pendingImpact.CurrentDate,
                previousWeather: pendingImpact.PreviousWeather,
                currentWeather: pendingImpact.CurrentWeather,
                environment: pendingImpact.Environment);

            int healthDelta = PendingWeatherHealthImpactStep.CalculateHealthDelta(
                person: resident,
                pendingImpacts: [pendingImpact],
                weatherImpactPolicy: policy);

            Assert.True(expected.HealthDelta < 0);
            Assert.Equal(expected.HealthDelta, healthDelta);
            Assert.Equal(80, resident.Health.Value);
        }

        [Fact]
        public void CalculateHealthDelta_WhenTransitionsAccumulate_SumsMedicalPressureOnly()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(1960, 7, 10),
                currentDate: CurrentDate);
            var policy = new CityPopulationWeatherImpactPolicy(
                new CityPopulationClimateAdaptationPolicy());
            CityPopulationPendingWeatherImpact first = CreatePendingImpact(Guid.NewGuid());
            CityPopulationPendingWeatherImpact second = CreatePendingImpact(Guid.NewGuid());
            int expected = policy.CalculateDifferential(
                               resident,
                               first.CurrentDate,
                               first.PreviousWeather,
                               first.CurrentWeather,
                               first.Environment)
                          .HealthDelta * 2;

            int healthDelta = PendingWeatherHealthImpactStep.CalculateHealthDelta(
                person: resident,
                pendingImpacts: [first, second],
                weatherImpactPolicy: policy);

            Assert.Equal(expected, healthDelta);
            Assert.Equal(50, resident.Happiness.Value);
        }

        private static CityPopulationPendingWeatherImpact CreatePendingImpact(
            Guid impactId,
            CityPopulationEnvironment? environment = null)
        {
            return CityPopulationPendingWeatherImpact.Create(
                impactId: impactId,
                cityId: CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                currentDate: CurrentDate,
                previousWeather: CreateWeather(
                    PopulationWeatherType.Clear,
                    PopulationWeatherSeverity.Calm,
                    22m),
                currentWeather: CreateWeather(
                    PopulationWeatherType.Heatwave,
                    PopulationWeatherSeverity.Extreme,
                    39m),
                environment: environment,
                occurredAtUtc: OccurredAtUtc);
        }

        private static WeatherImpactProfile CreateWeather(
            PopulationWeatherType type,
            PopulationWeatherSeverity severity,
            decimal temperatureC)
        {
            return new WeatherImpactProfile(
                Type: type,
                Severity: severity,
                PrecipitationKind: PopulationPrecipitationKind.None,
                TemperatureC: temperatureC,
                HumidityPercent: 45m,
                WindSpeedKph: 12m,
                CloudCoveragePercent: 35m,
                PressureHpa: 1012m);
        }
    }
}

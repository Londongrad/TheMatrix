using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationPendingWeatherImpactTests
    {
        [Fact]
        public void Create_PreservesWeatherAndEnvironmentSnapshots()
        {
            var cityId = CityId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var occurredAtUtc = new DateTimeOffset(2048, 7, 10, 12, 0, 0, TimeSpan.Zero);
            WeatherImpactProfile previousWeather = CreateWeather(
                PopulationWeatherType.Clear,
                PopulationWeatherSeverity.Mild,
                22m);
            WeatherImpactProfile currentWeather = CreateWeather(
                PopulationWeatherType.Heatwave,
                PopulationWeatherSeverity.Extreme,
                39m);
            CityPopulationEnvironment environment = CityPopulationEnvironment.Create(
                cityId: cityId,
                climateZone: PopulationClimateZone.Tropical,
                hemisphere: PopulationHemisphere.Southern,
                utcOffsetMinutes: 180,
                createdAtUtc: occurredAtUtc.AddDays(-1));

            CityPopulationPendingWeatherImpact impact = CityPopulationPendingWeatherImpact.Create(
                impactId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                cityId: cityId,
                currentDate: new DateOnly(2048, 7, 10),
                previousWeather: previousWeather,
                currentWeather: currentWeather,
                environment: environment,
                occurredAtUtc: occurredAtUtc);

            Assert.Equal(previousWeather, impact.PreviousWeather);
            Assert.Equal(currentWeather, impact.CurrentWeather);
            Assert.NotNull(impact.Environment);
            Assert.Equal(environment.ClimateZone, impact.Environment!.ClimateZone);
            Assert.Equal(environment.Hemisphere, impact.Environment.Hemisphere);
            Assert.Equal(environment.UtcOffsetMinutes, impact.Environment.UtcOffsetMinutes);
            Assert.Equal(occurredAtUtc, impact.OccurredAtUtc);
        }

        [Fact]
        public void Create_WithoutEnvironment_PreservesNeutralAdaptation()
        {
            WeatherImpactProfile weather = CreateWeather(
                PopulationWeatherType.Clear,
                PopulationWeatherSeverity.Calm,
                20m);

            CityPopulationPendingWeatherImpact impact = CityPopulationPendingWeatherImpact.Create(
                impactId: Guid.NewGuid(),
                cityId: CityId.From(Guid.NewGuid()),
                currentDate: new DateOnly(2048, 7, 10),
                previousWeather: weather,
                currentWeather: weather,
                environment: null,
                occurredAtUtc: new DateTimeOffset(2048, 7, 10, 12, 0, 0, TimeSpan.Zero));

            Assert.Null(impact.Environment);
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

using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Weather
{
    public sealed class WeatherStatePlannerTests
    {
        private readonly WeatherStatePlanner _planner = new();

        [Fact]
        public void PlanNaturalState_WithSameInputs_IsDeterministicAndAlignsToSixHourWindow()
        {
            var environment = CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Northern,
                utcOffset: CityUtcOffset.FromMinutes(180));
            WeatherClimateProfile climateProfile = WeatherTestSupport.CreateClimateProfile();
            var generationSeed = new CityGenerationSeed("alpha-seed");
            var evaluatedAt = SimTime.FromUtc(DateTimeOffset.Parse("2048-06-01T10:34:00+00:00"));

            WeatherState first = _planner.PlanNaturalState(
                environment: environment,
                climateProfile: climateProfile,
                generationSeed: generationSeed,
                evaluatedAt: evaluatedAt);
            WeatherState second = _planner.PlanNaturalState(
                environment: environment,
                climateProfile: climateProfile,
                generationSeed: generationSeed,
                evaluatedAt: evaluatedAt);

            Assert.Equal(
                expected: first,
                actual: second);
            Assert.Equal(
                expected: DateTimeOffset.Parse("2048-06-01T09:00:00+00:00"),
                actual: first.StartedAt.ValueUtc);
            Assert.Equal(
                expected: DateTimeOffset.Parse("2048-06-01T15:00:00+00:00"),
                actual: first.ExpectedUntil.ValueUtc);
            Assert.True(first.IsActiveAt(evaluatedAt));
        }

        [Fact]
        public void PlanNaturalState_WithSouthernHemisphereInJanuary_IsWarmerThanNorthern()
        {
            WeatherClimateProfile climateProfile = WeatherTestSupport.CreateClimateProfile();
            var generationSeed = new CityGenerationSeed("alpha-seed");
            var evaluatedAt = SimTime.FromUtc(DateTimeOffset.Parse("2048-01-15T12:00:00+00:00"));
            var northernEnvironment = CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Northern,
                utcOffset: CityUtcOffset.FromMinutes(0));
            var southernEnvironment = CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Southern,
                utcOffset: CityUtcOffset.FromMinutes(0));

            WeatherState northern = _planner.PlanNaturalState(
                environment: northernEnvironment,
                climateProfile: climateProfile,
                generationSeed: generationSeed,
                evaluatedAt: evaluatedAt);
            WeatherState southern = _planner.PlanNaturalState(
                environment: southernEnvironment,
                climateProfile: climateProfile,
                generationSeed: generationSeed,
                evaluatedAt: evaluatedAt);

            Assert.True(southern.Temperature.Value > northern.Temperature.Value);
            Assert.NotEqual(
                expected: northern,
                actual: southern);
            Assert.Equal(
                expected: DateTimeOffset.Parse("2048-01-15T12:00:00+00:00"),
                actual: northern.StartedAt.ValueUtc);
            Assert.Equal(
                expected: DateTimeOffset.Parse("2048-01-15T18:00:00+00:00"),
                actual: northern.ExpectedUntil.ValueUtc);
        }
    }
}

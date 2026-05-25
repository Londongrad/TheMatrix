using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CreateCity
{
    public sealed class CreateCityCommandValidatorTests
    {
        private readonly CreateCityCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidManualWeatherRequest_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(CreateCommand());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidSimulationEnvironmentAndClockValues_ReturnsErrors()
        {
            CreateCityCommand command = CreateCommand() with
            {
                SimulationKind = "Arcology",
                ClimateZone = "Temperate-ish",
                Hemisphere = "Up",
                UtcOffsetMinutes = 17,
                StartSimTimeUtc = new DateTimeOffset(
                    year: 2048,
                    month: 8,
                    day: 1,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3))
            };

            ValidationResult? result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "SimulationKind");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "ClimateZone");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "Hemisphere");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "UtcOffsetMinutes");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "StartSimTimeUtc");
        }

        [Fact]
        public void Validate_WithInvalidManualWeatherAndPopulationValues_ReturnsErrors()
        {
            CreateCityCommand command = CreateCommand() with
            {
                InitialWeatherType = "SolarFlare",
                InitialWeatherSeverity = "Impossible",
                InitialWeatherTemperatureC = 999m,
                PlannedPeopleCount = 1_000_001
            };

            ValidationResult? result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "InitialWeatherType");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "InitialWeatherSeverity");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "InitialWeatherTemperatureC");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "PlannedPeopleCount");
        }

        private static CreateCityCommand CreateCommand()
        {
            return new CreateCityCommand(
                Name: "Neo Tokyo",
                SimulationKind: "ClassicCity",
                ClimateZone: "Temperate",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180,
                GenerationSeed: "neo-tokyo-seed",
                SizeTier: "Medium",
                UrbanDensity: "Balanced",
                DevelopmentLevel: "Balanced",
                EconomyProfile: "Balanced",
                PopulationOccupancyProfile: "Balanced",
                InitialWeatherMode: "Manual",
                InitialWeatherType: "Clear",
                InitialWeatherSeverity: "Calm",
                InitialWeatherTemperatureC: 18m,
                StartSimTimeUtc: DateTimeOffset.Parse("2048-08-01T09:00:00+00:00"),
                SpeedMultiplier: 60m,
                PlannedPeopleCount: 25_000,
                ProvisioningCorrelationId: Guid.NewGuid(),
                ScenarioModelSetVersion: "classic-city-v3");
        }
    }
}

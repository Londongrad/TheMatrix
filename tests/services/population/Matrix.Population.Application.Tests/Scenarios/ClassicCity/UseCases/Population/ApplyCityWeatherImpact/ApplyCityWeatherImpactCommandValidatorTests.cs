using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed class ApplyCityWeatherImpactCommandValidatorTests
    {
        private readonly ApplyCityWeatherImpactCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(CreateCommand());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidFields_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                CreateCommand() with
                {
                    CityId = Guid.Empty,
                    IntegrationMessageId = Guid.Empty,
                    ConsumerName = "",
                    AtSimTimeUtc = new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    OccurredOnUtc = DateTime.SpecifyKind(
                        value: new DateTime(
                            year: 2048,
                            month: 5,
                            day: 3,
                            hour: 18,
                            minute: 0,
                            second: 0),
                        kind: DateTimeKind.Local),
                    PreviousState = null!,
                    CurrentState = null!
                });

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "IntegrationMessageId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ConsumerName");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "AtSimTimeUtc");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "OccurredOnUtc");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "PreviousState");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CurrentState");
        }

        private static ApplyCityWeatherImpactCommand CreateCommand()
        {
            return new ApplyCityWeatherImpactCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-weather",
                AtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 18,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc),
                PreviousState: CreateSnapshot(
                    type: "Clear",
                    severity: "Calm"),
                CurrentState: CreateSnapshot(
                    type: "Storm",
                    severity: "Severe"));
        }

        private static WeatherImpactSnapshotInput CreateSnapshot(
            string type,
            string severity)
        {
            return new WeatherImpactSnapshotInput(
                Type: type,
                Severity: severity,
                PrecipitationKind: "Rain",
                TemperatureC: 16m,
                HumidityPercent: 65m,
                WindSpeedKph: 25m,
                CloudCoveragePercent: 70m,
                PressureHpa: 1008m);
        }
    }
}

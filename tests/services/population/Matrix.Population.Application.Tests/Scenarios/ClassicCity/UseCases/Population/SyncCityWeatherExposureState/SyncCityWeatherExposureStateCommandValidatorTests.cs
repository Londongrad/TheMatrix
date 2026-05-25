using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState
{
    public sealed class SyncCityWeatherExposureStateCommandValidatorTests
    {
        private readonly SyncCityWeatherExposureStateCommandValidator _validator = new();

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
                        hour: 19,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    OccurredOnUtc = DateTime.SpecifyKind(
                        value: new DateTime(
                            year: 2048,
                            month: 5,
                            day: 3,
                            hour: 19,
                            minute: 30,
                            second: 0),
                        kind: DateTimeKind.Local),
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
                filter: x => x.PropertyName == "CurrentState");
        }

        private static SyncCityWeatherExposureStateCommand CreateCommand()
        {
            return new SyncCityWeatherExposureStateCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-weather-exposure",
                AtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 19,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 19,
                    minute: 30,
                    second: 0,
                    kind: DateTimeKind.Utc),
                PreviousState: null,
                CurrentState: new WeatherImpactSnapshotInput(
                    Type: "Rain",
                    Severity: "Moderate",
                    PrecipitationKind: "Rain",
                    TemperatureC: 12m,
                    HumidityPercent: 75m,
                    WindSpeedKph: 18m,
                    CloudCoveragePercent: 82m,
                    PressureHpa: 1002m));
        }
    }
}

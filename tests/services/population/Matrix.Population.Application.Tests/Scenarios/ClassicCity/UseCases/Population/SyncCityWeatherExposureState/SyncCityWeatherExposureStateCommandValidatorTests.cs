using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;

public sealed class SyncCityWeatherExposureStateCommandValidatorTests
{
    private readonly SyncCityWeatherExposureStateCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsErrors()
    {
        var result = _validator.Validate(CreateCommand() with
        {
            CityId = Guid.Empty,
            IntegrationMessageId = Guid.Empty,
            ConsumerName = "",
            AtSimTimeUtc = new DateTimeOffset(2048, 5, 3, 19, 30, 0, TimeSpan.FromHours(3)),
            OccurredOnUtc = DateTime.SpecifyKind(new DateTime(2048, 5, 3, 19, 30, 0), DateTimeKind.Local),
            CurrentState = null!
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "IntegrationMessageId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ConsumerName");
        Assert.Contains(result.Errors, x => x.PropertyName == "AtSimTimeUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "OccurredOnUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "CurrentState");
    }

    private static SyncCityWeatherExposureStateCommand CreateCommand()
    {
        return new SyncCityWeatherExposureStateCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-weather-exposure",
            AtSimTimeUtc: new DateTimeOffset(2048, 5, 3, 19, 30, 0, TimeSpan.Zero),
            OccurredOnUtc: new DateTime(2048, 5, 3, 19, 30, 0, DateTimeKind.Utc),
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

using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;

public sealed class ApplyCityWeatherImpactCommandValidatorTests
{
    private readonly ApplyCityWeatherImpactCommandValidator _validator = new();

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
            AtSimTimeUtc = new DateTimeOffset(2048, 5, 3, 18, 0, 0, TimeSpan.FromHours(3)),
            OccurredOnUtc = DateTime.SpecifyKind(new DateTime(2048, 5, 3, 18, 0, 0), DateTimeKind.Local),
            PreviousState = null!,
            CurrentState = null!
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "IntegrationMessageId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ConsumerName");
        Assert.Contains(result.Errors, x => x.PropertyName == "AtSimTimeUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "OccurredOnUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "PreviousState");
        Assert.Contains(result.Errors, x => x.PropertyName == "CurrentState");
    }

    private static ApplyCityWeatherImpactCommand CreateCommand()
    {
        return new ApplyCityWeatherImpactCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-weather",
            AtSimTimeUtc: new DateTimeOffset(2048, 5, 3, 18, 0, 0, TimeSpan.Zero),
            OccurredOnUtc: new DateTime(2048, 5, 3, 18, 0, 0, DateTimeKind.Utc),
            PreviousState: CreateSnapshot("Clear", "Calm"),
            CurrentState: CreateSnapshot("Storm", "Severe"));
    }

    private static WeatherImpactSnapshotInput CreateSnapshot(string type, string severity)
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

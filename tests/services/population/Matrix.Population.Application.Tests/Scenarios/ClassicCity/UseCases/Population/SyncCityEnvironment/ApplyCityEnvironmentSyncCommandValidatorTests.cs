using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;

public sealed class ApplyCityEnvironmentSyncCommandValidatorTests
{
    private readonly ApplyCityEnvironmentSyncCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new ApplyCityEnvironmentSyncCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            ClimateZone: "Continental",
            Hemisphere: "Southern",
            UtcOffsetMinutes: -120,
            SyncedAtUtc: new DateTimeOffset(2048, 5, 3, 17, 5, 0, TimeSpan.Zero)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsErrors()
    {
        var result = _validator.Validate(new ApplyCityEnvironmentSyncCommand(
            CityId: Guid.Empty,
            ClimateZone: "",
            Hemisphere: "",
            UtcOffsetMinutes: -1000,
            SyncedAtUtc: new DateTimeOffset(2048, 5, 3, 17, 5, 0, TimeSpan.FromHours(-4))));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ClimateZone");
        Assert.Contains(result.Errors, x => x.PropertyName == "Hemisphere");
        Assert.Contains(result.Errors, x => x.PropertyName == "UtcOffsetMinutes");
        Assert.Contains(result.Errors, x => x.PropertyName == "SyncedAtUtc");
    }
}

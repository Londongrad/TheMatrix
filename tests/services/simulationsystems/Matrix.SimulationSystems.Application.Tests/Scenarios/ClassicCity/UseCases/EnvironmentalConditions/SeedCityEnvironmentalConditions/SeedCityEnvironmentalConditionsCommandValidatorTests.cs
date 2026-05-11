using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions;

public sealed class SeedCityEnvironmentalConditionsCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new SeedCityEnvironmentalConditionsCommandValidator();

        var result = validator.Validate(new SeedCityEnvironmentalConditionsCommand(
            CityId: Guid.Parse("cccccccc-dddd-eeee-ffff-aaaaaaaaaaaa"),
            CreatedAtUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.Zero),
            SimulationKind: "ClassicCity",
            DevelopmentLevel: "standard"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var validator = new SeedCityEnvironmentalConditionsCommandValidator();

        var result = validator.Validate(new SeedCityEnvironmentalConditionsCommand(
            CityId: Guid.Empty,
            CreatedAtUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.FromHours(3)),
            SimulationKind: "ClassicCity",
            DevelopmentLevel: "standard"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "CreatedAtUtc");
    }
}

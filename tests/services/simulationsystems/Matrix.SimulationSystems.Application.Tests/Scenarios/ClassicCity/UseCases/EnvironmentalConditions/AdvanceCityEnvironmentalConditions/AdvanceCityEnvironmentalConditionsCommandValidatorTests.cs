using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions;

public sealed class AdvanceCityEnvironmentalConditionsCommandValidatorTests
{
    private readonly AdvanceCityEnvironmentalConditionsCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new AdvanceCityEnvironmentalConditionsCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            FromSimTimeUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.Zero),
            ToSimTimeUtc: new DateTimeOffset(2052, 3, 4, 9, 0, 0, TimeSpan.Zero),
            TickId: 4));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var result = _validator.Validate(new AdvanceCityEnvironmentalConditionsCommand(
            CityId: Guid.Empty,
            FromSimTimeUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.FromHours(3)),
            ToSimTimeUtc: new DateTimeOffset(2052, 3, 4, 7, 59, 0, TimeSpan.FromHours(3)),
            TickId: 4));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "FromSimTimeUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "ToSimTimeUtc");
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("ToSimTimeUtc must be greater than FromSimTimeUtc."));
    }
}

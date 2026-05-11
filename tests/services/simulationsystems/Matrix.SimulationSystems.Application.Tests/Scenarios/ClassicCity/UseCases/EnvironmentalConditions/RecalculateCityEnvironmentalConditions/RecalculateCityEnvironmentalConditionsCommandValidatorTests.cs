using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions;

public sealed class RecalculateCityEnvironmentalConditionsCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new RecalculateCityEnvironmentalConditionsCommandValidator();

        var result = validator.Validate(new RecalculateCityEnvironmentalConditionsCommand(
            CityId: Guid.Parse("dddddddd-eeee-ffff-aaaa-bbbbbbbbbbbb"),
            AtSimTimeUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
            Weather: SimulationSystemsApplicationTestSupport.CreateWeather()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var validator = new RecalculateCityEnvironmentalConditionsCommandValidator();

        var result = validator.Validate(new RecalculateCityEnvironmentalConditionsCommand(
            CityId: Guid.Empty,
            AtSimTimeUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.FromHours(3)),
            Weather: null!));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "AtSimTimeUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "Weather");
    }
}

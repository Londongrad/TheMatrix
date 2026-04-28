using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.UpdateCityEnvironment;

public sealed class UpdateCityEnvironmentCommandValidatorTests
{
    private readonly UpdateCityEnvironmentCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsNoErrors()
    {
        var result = _validator.Validate(new UpdateCityEnvironmentCommand(Guid.NewGuid(), "Temperate", "Northern", 180));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidValues_ReturnsErrors()
    {
        var result = _validator.Validate(new UpdateCityEnvironmentCommand(Guid.Empty, "Mars", "Up", 17));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ClimateZone");
        Assert.Contains(result.Errors, error => error.PropertyName == "Hemisphere");
        Assert.Contains(result.Errors, error => error.PropertyName == "UtcOffsetMinutes");
    }
}

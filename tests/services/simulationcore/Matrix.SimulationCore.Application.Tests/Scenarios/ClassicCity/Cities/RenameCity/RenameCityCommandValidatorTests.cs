using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RenameCity;

public sealed class RenameCityCommandValidatorTests
{
    private readonly RenameCityCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsNoErrors()
    {
        var result = _validator.Validate(new RenameCityCommand(Guid.NewGuid(), "Neo Tokyo"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidValues_ReturnsErrors()
    {
        var result = _validator.Validate(new RenameCityCommand(Guid.Empty, ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Name");
    }
}

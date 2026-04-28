using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailPopulationBootstrap;

public sealed class FailCityPopulationBootstrapCommandValidatorTests
{
    private readonly FailCityPopulationBootstrapCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsNoErrors()
    {
        var result = _validator.Validate(new FailCityPopulationBootstrapCommand(Guid.NewGuid(), Guid.NewGuid(), "TIMEOUT"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidValues_ReturnsErrors()
    {
        var result = _validator.Validate(new FailCityPopulationBootstrapCommand(Guid.Empty, Guid.Empty, ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "OperationId");
        Assert.Contains(result.Errors, error => error.PropertyName == "FailureCode");
    }
}

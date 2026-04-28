using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailEconomyBootstrap;

public sealed class FailCityEconomyBootstrapCommandValidatorTests
{
    private readonly FailCityEconomyBootstrapCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsNoErrors()
    {
        var result = _validator.Validate(new FailCityEconomyBootstrapCommand(Guid.NewGuid(), Guid.NewGuid(), "CAPACITY_LIMIT"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidValues_ReturnsErrors()
    {
        var result = _validator.Validate(new FailCityEconomyBootstrapCommand(Guid.Empty, Guid.Empty, ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "OperationId");
        Assert.Contains(result.Errors, error => error.PropertyName == "FailureCode");
    }
}

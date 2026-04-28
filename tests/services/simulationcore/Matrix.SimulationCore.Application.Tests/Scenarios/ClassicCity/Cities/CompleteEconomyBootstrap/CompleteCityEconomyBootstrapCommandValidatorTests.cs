using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CompleteEconomyBootstrap;

public sealed class CompleteCityEconomyBootstrapCommandValidatorTests
{
    private readonly CompleteCityEconomyBootstrapCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsNoErrors()
    {
        var result = _validator.Validate(new CompleteCityEconomyBootstrapCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidValues_ReturnsErrors()
    {
        var result = _validator.Validate(new CompleteCityEconomyBootstrapCommand(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "OperationId");
    }
}

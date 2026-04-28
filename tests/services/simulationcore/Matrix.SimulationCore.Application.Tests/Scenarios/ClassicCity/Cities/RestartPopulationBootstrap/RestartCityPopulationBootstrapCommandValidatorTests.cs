using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RestartPopulationBootstrap;

public sealed class RestartCityPopulationBootstrapCommandValidatorTests
{
    private readonly RestartCityPopulationBootstrapCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCityId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new RestartCityPopulationBootstrapCommand(Guid.NewGuid(), 1500));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyCityId_ReturnsError()
    {
        var result = _validator.Validate(new RestartCityPopulationBootstrapCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
    }
}

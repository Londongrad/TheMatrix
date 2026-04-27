using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityAnchors;

public sealed class GetCityAnchorsQueryValidatorTests
{
    private readonly GetCityAnchorsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidCityId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetCityAnchorsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyCityId_ReturnsError()
    {
        var result = _validator.Validate(new GetCityAnchorsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
    }
}

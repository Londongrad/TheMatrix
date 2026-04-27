using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityDistricts;

public sealed class GetCityDistrictsQueryValidatorTests
{
    private readonly GetCityDistrictsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidCityId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetCityDistrictsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyCityId_ReturnsError()
    {
        var result = _validator.Validate(new GetCityDistrictsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
    }
}

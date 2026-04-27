using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityResidentialBuildings;

public sealed class GetCityResidentialBuildingsQueryValidatorTests
{
    private readonly GetCityResidentialBuildingsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidCityAndDistrictIds_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetCityResidentialBuildingsQuery(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyCityId_ReturnsError()
    {
        var result = _validator.Validate(new GetCityResidentialBuildingsQuery(Guid.Empty, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
    }

    [Fact]
    public void Validate_WithEmptyDistrictId_ReturnsError()
    {
        var result = _validator.Validate(new GetCityResidentialBuildingsQuery(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "DistrictId.Value");
    }
}

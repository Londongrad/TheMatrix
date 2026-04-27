using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.ResolveCityRoute;

public sealed class ResolveCityRouteQueryValidatorTests
{
    private readonly ResolveCityRouteQueryValidator _validator = new();

    [Fact]
    public void Validate_WithSupportedNormalizedKindsAndProfile_ReturnsNoErrors()
    {
        var result = _validator.Validate(
            new ResolveCityRouteQuery(
                Guid.NewGuid(),
                "residential_building",
                Guid.NewGuid(),
                "city-anchor",
                Guid.NewGuid(),
                "service_vehicle"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithUnsupportedValues_ReturnsErrors()
    {
        var result = _validator.Validate(
            new ResolveCityRouteQuery(
                Guid.Empty,
                "mystery",
                Guid.Empty,
                "unknown",
                Guid.Empty,
                "teleport"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "FromId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ToId");
        Assert.Contains(result.Errors, error => error.PropertyName == "FromKind");
        Assert.Contains(result.Errors, error => error.PropertyName == "ToKind");
        Assert.Contains(result.Errors, error => error.PropertyName == "Profile");
    }
}

using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.ResolveCityRoute
{
    public sealed class ResolveCityRouteQueryValidatorTests
    {
        private readonly ResolveCityRouteQueryValidator _validator = new();

        [Fact]
        public void Validate_WithSupportedNormalizedKindsAndProfile_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ResolveCityRouteQuery(
                    CityId: Guid.NewGuid(),
                    FromKind: "residential_building",
                    FromId: Guid.NewGuid(),
                    ToKind: "city-anchor",
                    ToId: Guid.NewGuid(),
                    Profile: "service_vehicle"));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithUnsupportedValues_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ResolveCityRouteQuery(
                    CityId: Guid.Empty,
                    FromKind: "mystery",
                    FromId: Guid.Empty,
                    ToKind: "unknown",
                    ToId: Guid.Empty,
                    Profile: "teleport"));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "FromId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "ToId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "FromKind");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "ToKind");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "Profile");
        }
    }
}

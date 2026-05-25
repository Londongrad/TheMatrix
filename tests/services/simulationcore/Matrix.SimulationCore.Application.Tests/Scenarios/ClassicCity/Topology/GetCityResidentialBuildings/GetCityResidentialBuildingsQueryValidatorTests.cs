using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityResidentialBuildings
{
    public sealed class GetCityResidentialBuildingsQueryValidatorTests
    {
        private readonly GetCityResidentialBuildingsQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidCityAndDistrictIds_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentialBuildingsQuery(
                    CityId: Guid.NewGuid(),
                    DistrictId: Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentialBuildingsQuery(
                    CityId: Guid.Empty,
                    DistrictId: null));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
        }

        [Fact]
        public void Validate_WithEmptyDistrictId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentialBuildingsQuery(
                    CityId: Guid.NewGuid(),
                    DistrictId: Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "DistrictId.Value");
        }
    }
}

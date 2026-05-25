using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityMapTopology
{
    public sealed class GetCityMapTopologyQueryValidatorTests
    {
        private readonly GetCityMapTopologyQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidCityId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new GetCityMapTopologyQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetCityMapTopologyQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
        }
    }
}

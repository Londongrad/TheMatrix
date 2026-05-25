using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityRoadGraph
{
    public sealed class GetCityRoadGraphQueryValidatorTests
    {
        private readonly GetCityRoadGraphQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidCityId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new GetCityRoadGraphQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetCityRoadGraphQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
        }
    }
}

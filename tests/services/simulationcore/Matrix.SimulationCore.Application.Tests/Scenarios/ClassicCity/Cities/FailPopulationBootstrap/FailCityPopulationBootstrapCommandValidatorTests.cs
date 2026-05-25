using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapCommandValidatorTests
    {
        private readonly FailCityPopulationBootstrapCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidValues_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new FailCityPopulationBootstrapCommand(
                    CityId: Guid.NewGuid(),
                    OperationId: Guid.NewGuid(),
                    FailureCode: "TIMEOUT"));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidValues_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new FailCityPopulationBootstrapCommand(
                    CityId: Guid.Empty,
                    OperationId: Guid.Empty,
                    FailureCode: ""));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "OperationId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "FailureCode");
        }
    }
}

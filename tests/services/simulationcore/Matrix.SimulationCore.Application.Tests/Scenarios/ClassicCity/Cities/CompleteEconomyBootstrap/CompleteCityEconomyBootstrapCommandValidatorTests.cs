using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CompleteEconomyBootstrap
{
    public sealed class CompleteCityEconomyBootstrapCommandValidatorTests
    {
        private readonly CompleteCityEconomyBootstrapCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidValues_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new CompleteCityEconomyBootstrapCommand(
                    CityId: Guid.NewGuid(),
                    OperationId: Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidValues_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new CompleteCityEconomyBootstrapCommand(
                    CityId: Guid.Empty,
                    OperationId: Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "OperationId");
        }
    }
}

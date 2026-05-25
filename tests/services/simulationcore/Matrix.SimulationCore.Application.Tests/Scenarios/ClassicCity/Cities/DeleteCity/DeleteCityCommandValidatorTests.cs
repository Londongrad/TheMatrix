using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.DeleteCity
{
    public sealed class DeleteCityCommandValidatorTests
    {
        private readonly DeleteCityCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCityId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new DeleteCityCommand(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new DeleteCityCommand(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
        }
    }
}

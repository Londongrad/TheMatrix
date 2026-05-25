using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather.GetWeather
{
    public sealed class GetWeatherQueryValidatorTests
    {
        private readonly GetWeatherQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidCityId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new GetWeatherQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetWeatherQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
        }
    }
}

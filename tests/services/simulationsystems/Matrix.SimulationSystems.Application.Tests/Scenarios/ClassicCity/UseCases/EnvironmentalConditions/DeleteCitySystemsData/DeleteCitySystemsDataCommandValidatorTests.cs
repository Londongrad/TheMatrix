using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData
{
    public sealed class DeleteCitySystemsDataCommandValidatorTests
    {
        [Fact]
        public void Validate_WhenCityIdIsEmptyAndTimestampIsNotUtc_ReturnsErrors()
        {
            var validator = new DeleteCitySystemsDataCommandValidator();

            ValidationResult result = validator.Validate(
                new DeleteCitySystemsDataCommand(
                    CityId: Guid.Empty,
                    DeletedAtUtc: DateTimeOffset.Now));

            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
        }
    }
}

using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources;
using Xunit;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources
{
    public sealed class DeleteCityResourcesCommandValidatorTests
    {
        [Fact]
        public void Validate_WhenCityIdIsEmptyAndTimestampIsNotUtc_ReturnsErrors()
        {
            var validator = new DeleteCityResourcesCommandValidator();

            ValidationResult result = validator.Validate(
                new DeleteCityResourcesCommand(
                    CityId: Guid.Empty,
                    DeletedAtUtc: DateTimeOffset.Now));

            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
        }
    }
}

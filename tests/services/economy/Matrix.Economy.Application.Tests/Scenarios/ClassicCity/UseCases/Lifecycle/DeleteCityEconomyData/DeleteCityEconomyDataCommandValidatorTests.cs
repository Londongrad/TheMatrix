using FluentValidation.TestHelper;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData;
using Xunit;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData
{
    public sealed class DeleteCityEconomyDataCommandValidatorTests
    {
        private readonly DeleteCityEconomyDataCommandValidator _validator = new();

        [Fact]
        public void Validate_EmptyCityId_HasValidationError()
        {
            TestValidationResult<DeleteCityEconomyDataCommand> result = _validator.TestValidate(
                new DeleteCityEconomyDataCommand(Guid.Empty, DateTimeOffset.UtcNow));

            result.ShouldHaveValidationErrorFor(x => x.CityId);
        }

        [Fact]
        public void Validate_NonUtcDeletionTime_HasValidationError()
        {
            TestValidationResult<DeleteCityEconomyDataCommand> result = _validator.TestValidate(
                new DeleteCityEconomyDataCommand(
                    Guid.NewGuid(),
                    new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 2,
                        hour: 12,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3))));

            result.ShouldHaveValidationErrorFor(x => x.DeletedAtUtc);
        }
    }
}

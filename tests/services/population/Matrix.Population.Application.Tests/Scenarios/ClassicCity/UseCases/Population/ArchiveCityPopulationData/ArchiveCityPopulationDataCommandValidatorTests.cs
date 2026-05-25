using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData
{
    public sealed class ArchiveCityPopulationDataCommandValidatorTests
    {
        private readonly ArchiveCityPopulationDataCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(CreateCommand());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidFields_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                CreateCommand() with
                {
                    CityId = Guid.Empty,
                    IntegrationMessageId = Guid.Empty,
                    ConsumerName = "",
                    ArchivedAtUtc = new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 13,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3))
                });

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "IntegrationMessageId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ConsumerName");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ArchivedAtUtc");
        }

        private static ArchiveCityPopulationDataCommand CreateCommand()
        {
            return new ArchiveCityPopulationDataCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-archive",
                ArchivedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}

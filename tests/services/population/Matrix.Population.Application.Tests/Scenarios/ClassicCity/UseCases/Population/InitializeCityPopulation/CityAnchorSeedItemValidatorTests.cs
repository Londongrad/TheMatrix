using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class CityAnchorSeedItemValidatorTests
    {
        private readonly CityAnchorSeedItemValidator _validator = new();

        [Fact]
        public void Validate_WithValidItem_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(CreateItem());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidFields_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                CreateItem() with
                {
                    CityAnchorId = Guid.Empty,
                    DistrictId = Guid.Empty,
                    AccessRoadNodeId = Guid.Empty,
                    Name = "",
                    Type = "",
                    Capacity = -1,
                    CreatedAtUtc = new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3))
                });

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityAnchorId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "DistrictId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "AccessRoadNodeId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Name");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Type");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Capacity");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CreatedAtUtc");
        }

        private static CityAnchorSeedItem CreateItem()
        {
            return new CityAnchorSeedItem(
                CityAnchorId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                AccessRoadNodeId: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                Name: "Central Clinic",
                Type: "Hospital",
                Capacity: 80,
                PositionX: 10.5m,
                PositionY: 25.75m,
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}

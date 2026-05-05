using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;

public sealed class CityAnchorSeedItemValidatorTests
{
    private readonly CityAnchorSeedItemValidator _validator = new();

    [Fact]
    public void Validate_WithValidItem_ReturnsNoErrors()
    {
        var result = _validator.Validate(CreateItem());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsErrors()
    {
        var result = _validator.Validate(CreateItem() with
        {
            CityAnchorId = Guid.Empty,
            DistrictId = Guid.Empty,
            AccessRoadNodeId = Guid.Empty,
            Name = "",
            Type = "",
            Capacity = -1,
            CreatedAtUtc = new DateTimeOffset(2048, 5, 3, 8, 0, 0, TimeSpan.FromHours(3))
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityAnchorId");
        Assert.Contains(result.Errors, x => x.PropertyName == "DistrictId");
        Assert.Contains(result.Errors, x => x.PropertyName == "AccessRoadNodeId");
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Type");
        Assert.Contains(result.Errors, x => x.PropertyName == "Capacity");
        Assert.Contains(result.Errors, x => x.PropertyName == "CreatedAtUtc");
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
            CreatedAtUtc: new DateTimeOffset(2048, 5, 3, 8, 0, 0, TimeSpan.Zero));
    }
}

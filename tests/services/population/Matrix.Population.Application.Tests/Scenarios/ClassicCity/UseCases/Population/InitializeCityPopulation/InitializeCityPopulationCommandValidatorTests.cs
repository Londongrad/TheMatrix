using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;

public sealed class InitializeCityPopulationCommandValidatorTests
{
    private readonly InitializeCityPopulationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithNonUtcCreatedAt_ReturnsError()
    {
        var command = CreateCommand() with
        {
            CreatedAtUtc = new DateTimeOffset(2048, 5, 3, 9, 10, 11, TimeSpan.FromHours(3))
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CreatedAtUtc");
    }

    [Fact]
    public void Validate_WithInvalidPeopleCount_ReturnsError()
    {
        var result = _validator.Validate(CreateCommand() with { PeopleCount = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "PeopleCount");
    }

    [Fact]
    public void Validate_WithInvalidNestedItems_ReturnsNestedErrors()
    {
        InitializeCityPopulationCommand command = CreateCommand() with
        {
            Environment = new CityPopulationEnvironmentInput(
                ClimateZone: "",
                Hemisphere: "",
                UtcOffsetMinutes: 1000),
            Tuning = new CityPopulationBootstrapTuningInput(
                HousingPressurePercent: -1,
                EconomicStabilityPercent: 150,
                SocialVolatilityPercent: 25,
                FamilyFormationPercent: 30),
            CityAnchors =
            [
                new CityAnchorSeedItem(
                    CityAnchorId: Guid.Empty,
                    DistrictId: Guid.Empty,
                    AccessRoadNodeId: Guid.Empty,
                    Name: "",
                    Type: "",
                    Capacity: -1,
                    PositionX: 10m,
                    PositionY: 20m,
                    CreatedAtUtc: new DateTimeOffset(2048, 5, 3, 12, 0, 0, TimeSpan.FromHours(1)))
            ],
            ResidentialBuildings =
            [
                new ResidentialBuildingSeedItem(
                    ResidentialBuildingId: Guid.Empty,
                    DistrictId: Guid.Empty,
                    ResidentCapacity: 0)
            ]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Environment.ClimateZone");
        Assert.Contains(result.Errors, x => x.PropertyName == "Environment.Hemisphere");
        Assert.Contains(result.Errors, x => x.PropertyName == "Environment.UtcOffsetMinutes");
        Assert.Contains(result.Errors, x => x.PropertyName == "Tuning.HousingPressurePercent");
        Assert.Contains(result.Errors, x => x.PropertyName == "Tuning.EconomicStabilityPercent");
        Assert.Contains(result.Errors, x => x.PropertyName == "CityAnchors[0].CityAnchorId");
        Assert.Contains(result.Errors, x => x.PropertyName == "CityAnchors[0].CreatedAtUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "ResidentialBuildings[0].ResidentCapacity");
    }

    private static InitializeCityPopulationCommand CreateCommand()
    {
        return new InitializeCityPopulationCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            CurrentDate: new DateOnly(2048, 5, 3),
            CreatedAtUtc: new DateTimeOffset(2048, 5, 3, 9, 10, 11, TimeSpan.Zero),
            PeopleCount: 120,
            RandomSeed: 7,
            Environment: new CityPopulationEnvironmentInput(
                ClimateZone: "Temperate",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180),
            Tuning: new CityPopulationBootstrapTuningInput(
                HousingPressurePercent: 35,
                EconomicStabilityPercent: 60,
                SocialVolatilityPercent: 25,
                FamilyFormationPercent: 40),
            CityAnchors:
            [
                new CityAnchorSeedItem(
                    CityAnchorId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    AccessRoadNodeId: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    Name: "Central Clinic",
                    Type: "Hospital",
                    Capacity: 80,
                    PositionX: 10.5m,
                    PositionY: 25.75m,
                    CreatedAtUtc: new DateTimeOffset(2048, 5, 3, 8, 0, 0, TimeSpan.Zero))
            ],
            ResidentialBuildings:
            [
                new ResidentialBuildingSeedItem(
                    ResidentialBuildingId: Guid.Parse("40000000-0000-0000-0000-000000000001"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    ResidentCapacity: 12)
            ]);
    }
}

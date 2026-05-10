using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEnvironmentalConditionPolicyTests
{
    [Fact]
    public void CreateSeed_ForSameInputs_IsDeterministic()
    {
        var policy = new CityEnvironmentalConditionPolicy();

        var left = policy.CreateSeed(
            cityId: SimulationSystemsTestData.CityId,
            developmentLevel: "standard",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);
        var right = policy.CreateSeed(
            cityId: SimulationSystemsTestData.CityId,
            developmentLevel: "standard",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);

        Assert.Equal(left.Drainage.LoadIndex, right.Drainage.LoadIndex);
        Assert.Equal(left.HeatingCoverageIndex.Value, right.HeatingCoverageIndex.Value);
        Assert.Equal(left.UtilityContinuityIndex.Value, right.UtilityContinuityIndex.Value);
    }

    [Fact]
    public void CreateSeed_WhenTimestampIsNotUtc_Throws()
    {
        var policy = new CityEnvironmentalConditionPolicy();

        Assert.ThrowsAny<Exception>(
            () => policy.CreateSeed(
                cityId: SimulationSystemsTestData.CityId,
                developmentLevel: "standard",
                asOfUtc: new DateTimeOffset(2051, 2, 3, 8, 0, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void CreateSeed_StrugglingCityStartsWeakerThanAdvancedCity()
    {
        var policy = new CityEnvironmentalConditionPolicy();
        var struggling = policy.CreateSeed(
            cityId: SimulationSystemsTestData.CityId,
            developmentLevel: "struggling",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);
        var advanced = policy.CreateSeed(
            cityId: Guid.Parse("73000000-0000-0000-0000-000000000002"),
            developmentLevel: "advanced",
            asOfUtc: SimulationSystemsTestData.CreatedAtUtc);

        Assert.True(struggling.DrainageInfrastructure.PumpCapacityIndex < advanced.DrainageInfrastructure.PumpCapacityIndex);
        Assert.True(struggling.HeatingInfrastructure.PlantCapacityIndex < advanced.HeatingInfrastructure.PlantCapacityIndex);
        Assert.True(struggling.RoadAccess.ServiceQualityIndex < advanced.RoadAccess.ServiceQualityIndex);
    }
}

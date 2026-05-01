using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEconomyCostProfilePolicyTests
{
    [Fact]
    public void CreateSeed_WhenSimulationKindIsNotClassicCity_ReturnsNeutralSnapshot()
    {
        DateTimeOffset asOfUtc = new(2048, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
            simulationKind: "metro",
            economyProfile: "struggling",
            asOfUtc: asOfUtc);

        Assert.Equal(1m, snapshot.WageMultiplier);
        Assert.Equal(1m, snapshot.RetailPriceMultiplier);
        Assert.Equal(1m, snapshot.HousingCostMultiplier);
        Assert.Equal(1m, snapshot.UtilityCostMultiplier);
        Assert.Equal(1m, snapshot.CostOfLivingIndex);
        Assert.Equal(1m, snapshot.AffordabilityIndex);
        Assert.Equal(asOfUtc, snapshot.EvaluatedAtUtc);
    }

    [Fact]
    public void CreateSeed_WhenEconomyProfileIsStruggling_ReturnsStrugglingPreset()
    {
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
            simulationKind: " classiccity ",
            economyProfile: " struggling ",
            asOfUtc: new DateTimeOffset(2048, 3, 4, 5, 6, 7, TimeSpan.Zero));

        Assert.Equal(0.86m, snapshot.WageMultiplier);
        Assert.Equal(0.94m, snapshot.RetailPriceMultiplier);
        Assert.Equal(0.97m, snapshot.HousingCostMultiplier);
        Assert.Equal(0.98m, snapshot.UtilityCostMultiplier);
        Assert.Equal(0.9633m, snapshot.CostOfLivingIndex);
        Assert.Equal(0.8928m, snapshot.AffordabilityIndex);
    }

    [Fact]
    public void CreateSeed_WhenEconomyProfileIsAffluent_ReturnsAffluentPreset()
    {
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
            simulationKind: "CLASSICCITY",
            economyProfile: "AFFLUENT",
            asOfUtc: new DateTimeOffset(2048, 3, 4, 5, 6, 7, TimeSpan.Zero));

        Assert.Equal(1.18m, snapshot.WageMultiplier);
        Assert.Equal(1.08m, snapshot.RetailPriceMultiplier);
        Assert.Equal(1.14m, snapshot.HousingCostMultiplier);
        Assert.Equal(1.07m, snapshot.UtilityCostMultiplier);
        Assert.Equal(1.0967m, snapshot.CostOfLivingIndex);
        Assert.Equal(1.0760m, snapshot.AffordabilityIndex);
    }
}

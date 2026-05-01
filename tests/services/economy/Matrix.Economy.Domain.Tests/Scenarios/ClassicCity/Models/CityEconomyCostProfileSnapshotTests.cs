using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Models;

public sealed class CityEconomyCostProfileSnapshotTests
{
    [Fact]
    public void Neutral_WhenCreated_ReturnsUnitMultipliersAndIndexes()
    {
        DateTimeOffset evaluatedAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);

        CityEconomyCostProfileSnapshot snapshot = CityEconomyCostProfileSnapshot.Neutral(evaluatedAtUtc);

        Assert.Equal(1m, snapshot.WageMultiplier);
        Assert.Equal(1m, snapshot.RetailPriceMultiplier);
        Assert.Equal(1m, snapshot.HousingCostMultiplier);
        Assert.Equal(1m, snapshot.UtilityCostMultiplier);
        Assert.Equal(1m, snapshot.CostOfLivingIndex);
        Assert.Equal(1m, snapshot.AffordabilityIndex);
        Assert.Equal(evaluatedAtUtc, snapshot.EvaluatedAtUtc);
    }

    [Theory]
    [InlineData(CityHouseholdObligationKind.Rent, 1.4)]
    [InlineData(CityHouseholdObligationKind.Utilities, 0.8)]
    [InlineData(CityHouseholdObligationKind.ServiceFee, 1.2)]
    public void ResolveObligationPriceMultiplier_WhenKnownKind_ReturnsMatchingMultiplier(
        CityHouseholdObligationKind kind,
        decimal expected)
    {
        var snapshot = new CityEconomyCostProfileSnapshot(
            WageMultiplier: 1m,
            RetailPriceMultiplier: 1.2m,
            HousingCostMultiplier: 1.4m,
            UtilityCostMultiplier: 0.8m,
            CostOfLivingIndex: 1.1m,
            AffordabilityIndex: 0.9m,
            EvaluatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));

        decimal result = snapshot.ResolveObligationPriceMultiplier(kind);

        Assert.Equal(expected, result);
    }
}

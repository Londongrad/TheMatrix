using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Models
{
    public sealed class CityEconomyCostProfileSnapshotTests
    {
        [Fact]
        public void Neutral_WhenCreated_ReturnsUnitMultipliersAndIndexes()
        {
            DateTimeOffset evaluatedAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);

            var snapshot = CityEconomyCostProfileSnapshot.Neutral(evaluatedAtUtc);

            Assert.Equal(
                expected: 1m,
                actual: snapshot.WageMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.HousingCostMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.CostOfLivingIndex);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.AffordabilityIndex);
            Assert.Equal(
                expected: evaluatedAtUtc,
                actual: snapshot.EvaluatedAtUtc);
        }

        [Theory]
        [InlineData(
            CityHouseholdObligationKind.Rent,
            1.4)]
        [InlineData(
            CityHouseholdObligationKind.Utilities,
            0.8)]
        [InlineData(
            CityHouseholdObligationKind.ServiceFee,
            1.2)]
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
                EvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));

            decimal result = snapshot.ResolveObligationPriceMultiplier(kind);

            Assert.Equal(
                expected: expected,
                actual: result);
        }
    }
}

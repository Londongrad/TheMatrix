using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Entities
{
    public sealed class CityEconomyCostProfileStateTests
    {
        [Fact]
        public void Create_WhenSeedIsValid_InitializesBaseAndCurrentValues()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CityEconomyCostProfileSnapshot seed = CreateSnapshot(
                wageMultiplier: 1.12345m,
                retailPriceMultiplier: 1.23456m,
                housingCostMultiplier: 1.34567m,
                utilityCostMultiplier: 1.45678m,
                costOfLivingIndex: 1.11119m,
                affordabilityIndex: 0.88881m,
                evaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));
            DateTimeOffset updatedAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 5,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            var state = CityEconomyCostProfileState.Create(
                cityId: cityId,
                seed: seed,
                updatedAtUtc: updatedAtUtc);

            Assert.Equal(
                expected: cityId,
                actual: state.CityId);
            Assert.Equal(
                expected: 1.1235m,
                actual: state.BaseWageMultiplier);
            Assert.Equal(
                expected: 1.2346m,
                actual: state.BaseRetailPriceMultiplier);
            Assert.Equal(
                expected: 1.3457m,
                actual: state.BaseHousingCostMultiplier);
            Assert.Equal(
                expected: 1.4568m,
                actual: state.BaseUtilityCostMultiplier);
            Assert.Equal(
                expected: 1.1235m,
                actual: state.WageMultiplier);
            Assert.Equal(
                expected: 1.2346m,
                actual: state.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1.3457m,
                actual: state.HousingCostMultiplier);
            Assert.Equal(
                expected: 1.4568m,
                actual: state.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1.1112m,
                actual: state.CostOfLivingIndex);
            Assert.Equal(
                expected: 0.8888m,
                actual: state.AffordabilityIndex);
            Assert.Equal(
                expected: seed.EvaluatedAtUtc,
                actual: state.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: updatedAtUtc,
                actual: state.UpdatedAtUtc);
        }

        [Fact]
        public void Create_WhenCityIdIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityEconomyCostProfileState.Create(
                cityId: Guid.Empty,
                seed: CreateSnapshot(),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 5,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
        }

        [Fact]
        public void Create_WhenSeedHasOutOfRangeMultiplier_ThrowsArgumentOutOfRangeException()
        {
            CityEconomyCostProfileSnapshot seed = CreateSnapshot(wageMultiplier: 0.39m);

            Assert.Throws<ArgumentOutOfRangeException>(() => CityEconomyCostProfileState.Create(
                cityId: Guid.NewGuid(),
                seed: seed,
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 5,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)));
        }

        [Fact]
        public void ApplySnapshot_WhenSnapshotIsValid_UpdatesCurrentValuesOnly()
        {
            var state = CityEconomyCostProfileState.Create(
                cityId: Guid.NewGuid(),
                seed: CreateSnapshot(
                    wageMultiplier: 1.1m,
                    retailPriceMultiplier: 1.2m,
                    housingCostMultiplier: 1.3m,
                    utilityCostMultiplier: 1.4m,
                    costOfLivingIndex: 1.5m,
                    affordabilityIndex: 0.9m,
                    evaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 3,
                        hour: 4,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityEconomyCostProfileSnapshot snapshot = CreateSnapshot(
                wageMultiplier: 1.23451m,
                retailPriceMultiplier: 1.11111m,
                housingCostMultiplier: 1.22222m,
                utilityCostMultiplier: 1.33333m,
                costOfLivingIndex: 1.44444m,
                affordabilityIndex: 0.77777m,
                evaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 4,
                    hour: 1,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            DateTimeOffset updatedAtUtc = new(
                year: 2048,
                month: 2,
                day: 4,
                hour: 2,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            state.ApplySnapshot(
                snapshot: snapshot,
                updatedAtUtc: updatedAtUtc);

            Assert.Equal(
                expected: 1.1m,
                actual: state.BaseWageMultiplier);
            Assert.Equal(
                expected: 1.2345m,
                actual: state.WageMultiplier);
            Assert.Equal(
                expected: 1.1111m,
                actual: state.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1.2222m,
                actual: state.HousingCostMultiplier);
            Assert.Equal(
                expected: 1.3333m,
                actual: state.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1.4444m,
                actual: state.CostOfLivingIndex);
            Assert.Equal(
                expected: 0.7778m,
                actual: state.AffordabilityIndex);
            Assert.Equal(
                expected: snapshot.EvaluatedAtUtc,
                actual: state.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: updatedAtUtc,
                actual: state.UpdatedAtUtc);
        }

        [Fact]
        public void ApplySnapshot_WhenTimestampIsNotUtc_ThrowsArgumentException()
        {
            var state = CityEconomyCostProfileState.Create(
                cityId: Guid.NewGuid(),
                seed: CreateSnapshot(),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 5,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityEconomyCostProfileSnapshot snapshot = CreateSnapshot(
                evaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 4,
                    hour: 1,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3)));

            Assert.Throws<ArgumentException>(() => state.ApplySnapshot(
                snapshot: snapshot,
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 4,
                    hour: 2,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)));
        }

        [Fact]
        public void ToSnapshot_WhenCalled_ReturnsCurrentValues()
        {
            var state = CityEconomyCostProfileState.Create(
                cityId: Guid.NewGuid(),
                seed: CreateSnapshot(),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 5,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityEconomyCostProfileSnapshot snapshot = CreateSnapshot(
                wageMultiplier: 1.3m,
                retailPriceMultiplier: 1.4m,
                housingCostMultiplier: 1.5m,
                utilityCostMultiplier: 1.6m,
                costOfLivingIndex: 1.7m,
                affordabilityIndex: 0.8m,
                evaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 4,
                    hour: 1,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            state.ApplySnapshot(
                snapshot: snapshot,
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 4,
                    hour: 2,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            CityEconomyCostProfileSnapshot result = state.ToSnapshot();

            Assert.Equal(
                expected: 1.3m,
                actual: result.WageMultiplier);
            Assert.Equal(
                expected: 1.4m,
                actual: result.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1.5m,
                actual: result.HousingCostMultiplier);
            Assert.Equal(
                expected: 1.6m,
                actual: result.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1.7m,
                actual: result.CostOfLivingIndex);
            Assert.Equal(
                expected: 0.8m,
                actual: result.AffordabilityIndex);
            Assert.Equal(
                expected: snapshot.EvaluatedAtUtc,
                actual: result.EvaluatedAtUtc);
        }

        private static CityEconomyCostProfileSnapshot CreateSnapshot(
            decimal wageMultiplier = 1.0m,
            decimal retailPriceMultiplier = 1.0m,
            decimal housingCostMultiplier = 1.0m,
            decimal utilityCostMultiplier = 1.0m,
            decimal costOfLivingIndex = 1.0m,
            decimal affordabilityIndex = 1.0m,
            DateTimeOffset? evaluatedAtUtc = null)
        {
            return new CityEconomyCostProfileSnapshot(
                WageMultiplier: wageMultiplier,
                RetailPriceMultiplier: retailPriceMultiplier,
                HousingCostMultiplier: housingCostMultiplier,
                UtilityCostMultiplier: utilityCostMultiplier,
                CostOfLivingIndex: costOfLivingIndex,
                AffordabilityIndex: affordabilityIndex,
                EvaluatedAtUtc: evaluatedAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));
        }
    }
}

using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates
{
    public sealed class CityHouseholdObligationTests
    {
        [Fact]
        public void Constructor_WhenArgumentsAreValid_NormalizesFieldsAndInitializesState()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            DateTimeOffset firstChargeDueAtUtc = new(
                year: 2048,
                month: 2,
                day: 10,
                hour: 0,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: cityId,
                kind: CityHouseholdObligationKind.Utilities,
                cadence: CityHouseholdObligationBillingCadence.Weekly,
                firstChargeDueAtUtc: firstChargeDueAtUtc,
                chargeAmount: 90m,
                taxAmount: 10m);

            Assert.Equal(
                expected: cityId,
                actual: obligation.CityId);
            Assert.Equal(
                expected: "Household obligation",
                actual: obligation.Name);
            Assert.Equal(
                expected: CityHouseholdObligationKind.Utilities,
                actual: obligation.Kind);
            Assert.Equal(
                expected: CityHouseholdObligationBillingCadence.Weekly,
                actual: obligation.BillingCadence);
            Assert.True(obligation.IsActive);
            Assert.Equal(
                expected: "CR",
                actual: obligation.UnitCode);
            Assert.Equal(
                expected: "Credits",
                actual: obligation.UnitDisplayName);
            Assert.Equal(
                expected: "$",
                actual: obligation.UnitSymbol);
            Assert.Equal(
                expected: Money.FromDecimal(90m),
                actual: obligation.ChargeAmount);
            Assert.Equal(
                expected: Money.FromDecimal(10m),
                actual: obligation.TaxAmount);
            Assert.Equal(
                expected: Money.FromDecimal(90m),
                actual: obligation.BaseChargeAmount);
            Assert.Equal(
                expected: Money.FromDecimal(10m),
                actual: obligation.BaseTaxAmount);
            Assert.Equal(
                expected: firstChargeDueAtUtc,
                actual: obligation.NextChargeDueAtUtc);
            Assert.Equal(
                expected: 0,
                actual: obligation.ChargeCount);
            Assert.Equal(
                expected: 0,
                actual: obligation.MissedChargeCount);
            Assert.False(obligation.HasActiveDelinquency);
        }

        [Fact]
        public void ResolveDueInstallmentCount_WhenDailyCadenceHasMultipleDays_ReturnsElapsedDayCount()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                cadence: CityHouseholdObligationBillingCadence.Daily,
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                chargeAmount: 30m,
                taxAmount: 3m);

            int installmentCount = obligation.ResolveDueInstallmentCount(
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 12,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            Assert.Equal(
                expected: 3,
                actual: installmentCount);
            Assert.Equal(
                expected: Money.FromDecimal(90m),
                actual: obligation.ResolveCurrentDueAmount(
                    new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 12,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            Assert.Equal(
                expected: Money.FromDecimal(9m),
                actual: obligation.ResolveCurrentDueTaxAmount(
                    new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 12,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
        }

        [Fact]
        public void ResolveDueInstallmentCount_WhenWeeklyCadenceHasElapsedWeeks_ReturnsElapsedWeekCount()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                cadence: CityHouseholdObligationBillingCadence.Weekly,
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            int installmentCount = obligation.ResolveDueInstallmentCount(
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 24,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            Assert.Equal(
                expected: 3,
                actual: installmentCount);
        }

        [Fact]
        public void ResolveDueInstallmentCount_WhenMonthlyCadenceCrossesMonthBoundary_ReturnsElapsedMonthCount()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 1,
                    day: 31,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            int installmentCount = obligation.ResolveDueInstallmentCount(
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 3,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            Assert.Equal(
                expected: 2,
                actual: installmentCount);
        }

        [Fact]
        public void Reprice_WhenMultiplierIsValid_UpdatesCurrentAmountsWithoutChangingBaseAmounts()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                chargeAmount: 80m,
                taxAmount: 12m);

            obligation.Reprice(multiplier: 1.25m);

            Assert.Equal(
                expected: Money.FromDecimal(100m),
                actual: obligation.ChargeAmount);
            Assert.Equal(
                expected: Money.FromDecimal(15m),
                actual: obligation.TaxAmount);
            Assert.Equal(
                expected: Money.FromDecimal(80m),
                actual: obligation.BaseChargeAmount);
            Assert.Equal(
                expected: Money.FromDecimal(12m),
                actual: obligation.BaseTaxAmount);
        }

        [Fact]
        public void Reprice_WhenMultiplierIsOutsideAllowedRange_ThrowsArgumentOutOfRangeException()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(cityId: Guid.NewGuid());

            Assert.Throws<ArgumentOutOfRangeException>(() => obligation.Reprice(multiplier: 0m));
            Assert.Throws<ArgumentOutOfRangeException>(() => obligation.Reprice(multiplier: 3.01m));
        }
    }
}

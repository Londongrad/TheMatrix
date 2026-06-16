using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates
{
    public sealed class CityHouseholdObligationLifecycleTests
    {
        [Fact]
        public void MarkCharged_WhenObligationHasDueInstallments_SettlesAllDueChargesAndResetsDelinquency()
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
                chargeAmount: 20m,
                taxAmount: 2m);
            DateTimeOffset chargedAtUtc = new(
                year: 2048,
                month: 2,
                day: 12,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            obligation.MarkChargeMissed(
                new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 11,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            obligation.MarkCharged(chargedAtUtc);

            Assert.Equal(
                expected: chargedAtUtc,
                actual: obligation.LastChargedAtUtc);
            Assert.Equal(
                expected: 3,
                actual: obligation.ChargeCount);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 13,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: obligation.NextChargeDueAtUtc);
            Assert.Equal(
                expected: 0,
                actual: obligation.MissedChargeCount);
            Assert.False(obligation.HasActiveDelinquency);
            Assert.False(obligation.HasServiceCutoff);
        }

        [Fact]
        public void MarkChargeMissed_WhenCalledTwiceOnSameDay_DoesNotIncrementMissedChargeCountTwice()
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
                    offset: TimeSpan.Zero));
            DateTimeOffset attemptedAtUtc = new(
                year: 2048,
                month: 2,
                day: 10,
                hour: 9,
                minute: 30,
                second: 0,
                offset: TimeSpan.Zero);

            obligation.MarkChargeMissed(attemptedAtUtc);
            obligation.MarkChargeMissed(attemptedAtUtc.AddHours(5));

            Assert.Equal(
                expected: 1,
                actual: obligation.MissedChargeCount);
            Assert.Equal(
                expected: attemptedAtUtc.AddHours(5),
                actual: obligation.LastChargeAttemptedAtUtc);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: obligation.FirstMissedChargeDueAtUtc);
        }

        [Fact]
        public void MarkChargeMissed_WhenUtilityDelinquencyReachesSecondBillingCycle_SetsServiceCutoff()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                kind: CityHouseholdObligationKind.Utilities,
                cadence: CityHouseholdObligationBillingCadence.Weekly,
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            DateTimeOffset cutoffAtUtc = new(
                year: 2048,
                month: 2,
                day: 17,
                hour: 9,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            obligation.MarkChargeMissed(
                new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            obligation.MarkChargeMissed(cutoffAtUtc);

            Assert.True(obligation.HasActiveDelinquency);
            Assert.True(obligation.HasServiceCutoff);
            Assert.Equal(
                expected: cutoffAtUtc,
                actual: obligation.ServiceCutoffAtUtc);
            Assert.Equal(
                expected: 2,
                actual: obligation.ResolveDelinquentBillingCycles(cutoffAtUtc));
        }

        [Fact]
        public void MarkChargeMissed_WhenRentDelinquencyEscalates_SetsEvictionNoticeAndEligibility()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                kind: CityHouseholdObligationKind.Rent,
                cadence: CityHouseholdObligationBillingCadence.Weekly,
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            DateTimeOffset noticeAtUtc = new(
                year: 2048,
                month: 2,
                day: 8,
                hour: 10,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            DateTimeOffset eligibleAtUtc = new(
                year: 2048,
                month: 2,
                day: 15,
                hour: 10,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            obligation.MarkChargeMissed(
                new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 1,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            obligation.MarkChargeMissed(noticeAtUtc);
            obligation.MarkChargeMissed(eligibleAtUtc);

            Assert.True(obligation.HasEvictionNotice);
            Assert.True(obligation.IsEvictionEligible);
            Assert.Equal(
                expected: noticeAtUtc,
                actual: obligation.EvictionNoticeIssuedAtUtc);
            Assert.Equal(
                expected: eligibleAtUtc,
                actual: obligation.EvictionEligibleAtUtc);
        }

        [Fact]
        public void Deactivate_WhenObligationIsInactive_DisablesDueResolutionAndFurtherCharging()
        {
            CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
                cityId: Guid.NewGuid(),
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            obligation.Deactivate();

            Assert.False(obligation.IsActive);
            Assert.False(
                obligation.IsDue(
                    new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 20,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            Assert.Equal(
                expected: 0,
                actual: obligation.ResolveDueInstallmentCount(
                    new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 20,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            Assert.Throws<InvalidOperationException>(() => obligation.MarkCharged(
                new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 20,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)));
        }
    }
}

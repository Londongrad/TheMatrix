using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates;

public sealed class CityHouseholdObligationLifecycleTests
{
    [Fact]
    public void MarkCharged_WhenObligationHasDueInstallments_SettlesAllDueChargesAndResetsDelinquency()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            cadence: CityHouseholdObligationBillingCadence.Daily,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero),
            chargeAmount: 20m,
            taxAmount: 2m);
        DateTimeOffset chargedAtUtc = new(2048, 2, 12, 12, 0, 0, TimeSpan.Zero);

        obligation.MarkChargeMissed(new DateTimeOffset(2048, 2, 11, 8, 0, 0, TimeSpan.Zero));
        obligation.MarkCharged(chargedAtUtc);

        Assert.Equal(chargedAtUtc, obligation.LastChargedAtUtc);
        Assert.Equal(3, obligation.ChargeCount);
        Assert.Equal(new DateTimeOffset(2048, 2, 13, 0, 0, 0, TimeSpan.Zero), obligation.NextChargeDueAtUtc);
        Assert.Equal(0, obligation.MissedChargeCount);
        Assert.False(obligation.HasActiveDelinquency);
        Assert.False(obligation.HasServiceCutoff);
    }

    [Fact]
    public void MarkChargeMissed_WhenCalledTwiceOnSameDay_DoesNotIncrementMissedChargeCountTwice()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            cadence: CityHouseholdObligationBillingCadence.Daily,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero));
        DateTimeOffset attemptedAtUtc = new(2048, 2, 10, 9, 30, 0, TimeSpan.Zero);

        obligation.MarkChargeMissed(attemptedAtUtc);
        obligation.MarkChargeMissed(attemptedAtUtc.AddHours(5));

        Assert.Equal(1, obligation.MissedChargeCount);
        Assert.Equal(attemptedAtUtc.AddHours(5), obligation.LastChargeAttemptedAtUtc);
        Assert.Equal(new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero), obligation.FirstMissedChargeDueAtUtc);
    }

    [Fact]
    public void MarkChargeMissed_WhenUtilityDelinquencyReachesSecondBillingCycle_SetsServiceCutoff()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            kind: CityHouseholdObligationKind.Utilities,
            cadence: CityHouseholdObligationBillingCadence.Weekly,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero));
        DateTimeOffset cutoffAtUtc = new(2048, 2, 17, 9, 0, 0, TimeSpan.Zero);

        obligation.MarkChargeMissed(new DateTimeOffset(2048, 2, 10, 8, 0, 0, TimeSpan.Zero));
        obligation.MarkChargeMissed(cutoffAtUtc);

        Assert.True(obligation.HasActiveDelinquency);
        Assert.True(obligation.HasServiceCutoff);
        Assert.Equal(cutoffAtUtc, obligation.ServiceCutoffAtUtc);
        Assert.Equal(2, obligation.ResolveDelinquentBillingCycles(cutoffAtUtc));
    }

    [Fact]
    public void MarkChargeMissed_WhenRentDelinquencyEscalates_SetsEvictionNoticeAndEligibility()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            kind: CityHouseholdObligationKind.Rent,
            cadence: CityHouseholdObligationBillingCadence.Weekly,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 1, 0, 0, 0, TimeSpan.Zero));
        DateTimeOffset noticeAtUtc = new(2048, 2, 8, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset eligibleAtUtc = new(2048, 2, 15, 10, 0, 0, TimeSpan.Zero);

        obligation.MarkChargeMissed(new DateTimeOffset(2048, 2, 1, 10, 0, 0, TimeSpan.Zero));
        obligation.MarkChargeMissed(noticeAtUtc);
        obligation.MarkChargeMissed(eligibleAtUtc);

        Assert.True(obligation.HasEvictionNotice);
        Assert.True(obligation.IsEvictionEligible);
        Assert.Equal(noticeAtUtc, obligation.EvictionNoticeIssuedAtUtc);
        Assert.Equal(eligibleAtUtc, obligation.EvictionEligibleAtUtc);
    }

    [Fact]
    public void Deactivate_WhenObligationIsInactive_DisablesDueResolutionAndFurtherCharging()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero));

        obligation.Deactivate();

        Assert.False(obligation.IsActive);
        Assert.False(obligation.IsDue(new DateTimeOffset(2048, 2, 20, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal(0, obligation.ResolveDueInstallmentCount(new DateTimeOffset(2048, 2, 20, 0, 0, 0, TimeSpan.Zero)));
        Assert.Throws<InvalidOperationException>(
            () => obligation.MarkCharged(new DateTimeOffset(2048, 2, 20, 0, 0, 0, TimeSpan.Zero)));
    }
}

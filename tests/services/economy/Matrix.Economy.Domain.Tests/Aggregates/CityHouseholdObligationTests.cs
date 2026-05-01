using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates;

public sealed class CityHouseholdObligationTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_NormalizesFieldsAndInitializesState()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        DateTimeOffset firstChargeDueAtUtc = new(2048, 2, 10, 0, 0, 0, TimeSpan.Zero);

        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: cityId,
            kind: CityHouseholdObligationKind.Utilities,
            cadence: CityHouseholdObligationBillingCadence.Weekly,
            firstChargeDueAtUtc: firstChargeDueAtUtc,
            chargeAmount: 90m,
            taxAmount: 10m);

        Assert.Equal(cityId, obligation.CityId);
        Assert.Equal("Household obligation", obligation.Name);
        Assert.Equal(CityHouseholdObligationKind.Utilities, obligation.Kind);
        Assert.Equal(CityHouseholdObligationBillingCadence.Weekly, obligation.BillingCadence);
        Assert.True(obligation.IsActive);
        Assert.Equal("CR", obligation.UnitCode);
        Assert.Equal("Credits", obligation.UnitDisplayName);
        Assert.Equal("$", obligation.UnitSymbol);
        Assert.Equal(Money.FromDecimal(90m), obligation.ChargeAmount);
        Assert.Equal(Money.FromDecimal(10m), obligation.TaxAmount);
        Assert.Equal(Money.FromDecimal(90m), obligation.BaseChargeAmount);
        Assert.Equal(Money.FromDecimal(10m), obligation.BaseTaxAmount);
        Assert.Equal(firstChargeDueAtUtc, obligation.NextChargeDueAtUtc);
        Assert.Equal(0, obligation.ChargeCount);
        Assert.Equal(0, obligation.MissedChargeCount);
        Assert.False(obligation.HasActiveDelinquency);
    }

    [Fact]
    public void ResolveDueInstallmentCount_WhenDailyCadenceHasMultipleDays_ReturnsElapsedDayCount()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            cadence: CityHouseholdObligationBillingCadence.Daily,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero),
            chargeAmount: 30m,
            taxAmount: 3m);

        int installmentCount = obligation.ResolveDueInstallmentCount(
            asOfUtc: new DateTimeOffset(2048, 2, 12, 8, 0, 0, TimeSpan.Zero));

        Assert.Equal(3, installmentCount);
        Assert.Equal(Money.FromDecimal(90m), obligation.ResolveCurrentDueAmount(new DateTimeOffset(2048, 2, 12, 8, 0, 0, TimeSpan.Zero)));
        Assert.Equal(Money.FromDecimal(9m), obligation.ResolveCurrentDueTaxAmount(new DateTimeOffset(2048, 2, 12, 8, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void ResolveDueInstallmentCount_WhenWeeklyCadenceHasElapsedWeeks_ReturnsElapsedWeekCount()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            cadence: CityHouseholdObligationBillingCadence.Weekly,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 2, 10, 0, 0, 0, TimeSpan.Zero));

        int installmentCount = obligation.ResolveDueInstallmentCount(
            asOfUtc: new DateTimeOffset(2048, 2, 24, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(3, installmentCount);
    }

    [Fact]
    public void ResolveDueInstallmentCount_WhenMonthlyCadenceCrossesMonthBoundary_ReturnsElapsedMonthCount()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            cadence: CityHouseholdObligationBillingCadence.Monthly,
            firstChargeDueAtUtc: new DateTimeOffset(2048, 1, 31, 0, 0, 0, TimeSpan.Zero));

        int installmentCount = obligation.ResolveDueInstallmentCount(
            asOfUtc: new DateTimeOffset(2048, 3, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(2, installmentCount);
    }

    [Fact]
    public void Reprice_WhenMultiplierIsValid_UpdatesCurrentAmountsWithoutChangingBaseAmounts()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(
            cityId: Guid.NewGuid(),
            chargeAmount: 80m,
            taxAmount: 12m);

        obligation.Reprice(multiplier: 1.25m);

        Assert.Equal(Money.FromDecimal(100m), obligation.ChargeAmount);
        Assert.Equal(Money.FromDecimal(15m), obligation.TaxAmount);
        Assert.Equal(Money.FromDecimal(80m), obligation.BaseChargeAmount);
        Assert.Equal(Money.FromDecimal(12m), obligation.BaseTaxAmount);
    }

    [Fact]
    public void Reprice_WhenMultiplierIsOutsideAllowedRange_ThrowsArgumentOutOfRangeException()
    {
        CityHouseholdObligation obligation = EconomyTestData.CreateObligation(cityId: Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => obligation.Reprice(multiplier: 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => obligation.Reprice(multiplier: 3.01m));
    }
}

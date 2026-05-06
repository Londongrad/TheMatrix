using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.Common;

public sealed class HouseholdObligationChargeSupportTests
{
    [Fact]
    public async Task TryChargeAsync_ReturnsNotDueWhenFrozenClockIsBeforeDueDate()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant Household", 300m);
        CityBusiness providerBusiness = CreateBusiness(cityId, "Landlord", CityBusinessKind.Landlord, 500m);
        CityHouseholdObligation obligation = CreateHouseholdObligation(
            cityId,
            householdAccount.Id,
            providerBusiness.Id,
            "Monthly Rent",
            CityHouseholdObligationKind.Rent,
            CityHouseholdObligationBillingCadence.Monthly,
            80m,
            8m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero));
        var support = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            timeProvider);

        HouseholdObligationChargeAttemptResult result = await support.TryChargeAsync(
            obligation,
            description: "pre-due check",
            occurredAtUtc: null,
            cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("NotDue", result.FailureCode);
        Assert.Empty(householdLedgerRepository.AddedEntries);
        Assert.Empty(businessLedgerRepository.AddedEntries);
        Assert.Equal(300m, householdAccount.Balance.Amount);
        Assert.Equal(500m, providerBusiness.Balance.Amount);
    }

    [Fact]
    public async Task TryChargeAsync_ChargesDueObligationWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant Household", 300m);
        CityBusiness providerBusiness = CreateBusiness(cityId, "Landlord", CityBusinessKind.Landlord, 500m);
        CityHouseholdObligation obligation = CreateHouseholdObligation(
            cityId,
            householdAccount.Id,
            providerBusiness.Id,
            "Monthly Rent",
            CityHouseholdObligationKind.Rent,
            CityHouseholdObligationBillingCadence.Monthly,
            80m,
            8m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 10, 0, 0, TimeSpan.Zero));
        var support = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            timeProvider);

        HouseholdObligationChargeAttemptResult result = await support.TryChargeAsync(
            obligation,
            description: "Rent collection",
            occurredAtUtc: null,
            cancellationToken: CancellationToken.None);

        var householdEntry = Assert.Single(householdLedgerRepository.AddedEntries);
        var businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
        Assert.True(result.Succeeded);
        Assert.Equal(80m, result.ChargedAmount.Amount);
        Assert.Equal(8m, result.ChargedTaxAmount.Amount);
        Assert.Equal(1, result.SettledInstallmentCount);
        Assert.Equal(timeProvider.UtcNow, householdEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal(220m, householdAccount.Balance.Amount);
        Assert.Equal(580m, providerBusiness.Balance.Amount);
        Assert.Equal(8m, providerBusiness.TaxReserve.Amount);
        Assert.Equal(72m, providerBusiness.TotalNetSalesRevenue.Amount);
        Assert.Equal(1, obligation.ChargeCount);
        Assert.Equal(new DateTimeOffset(2048, 6, 7, 9, 0, 0, TimeSpan.Zero), obligation.NextChargeDueAtUtc);
        Assert.Equal(obligation.Id.ToString("N"), result.LedgerEntry!.ReferenceCode);
    }

    [Fact]
    public async Task TryChargeAsync_MarksMissedChargeWhenBalanceIsInsufficient()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant Household", 20m);
        CityBusiness providerBusiness = CreateBusiness(cityId, "Utility Provider", CityBusinessKind.Utility, 500m);
        CityHouseholdObligation obligation = CreateHouseholdObligation(
            cityId,
            householdAccount.Id,
            providerBusiness.Id,
            "Power",
            CityHouseholdObligationKind.Utilities,
            CityHouseholdObligationBillingCadence.Monthly,
            80m,
            8m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 10, 0, 0, TimeSpan.Zero));
        var support = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            timeProvider);

        HouseholdObligationChargeAttemptResult result = await support.TryChargeAsync(
            obligation,
            description: null,
            occurredAtUtc: null,
            cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("InsufficientBalance", result.FailureCode);
        Assert.Equal(1, obligation.MissedChargeCount);
        Assert.Equal(timeProvider.UtcNow, obligation.LastChargeAttemptedAtUtc);
        Assert.Empty(householdLedgerRepository.AddedEntries);
        Assert.Empty(businessLedgerRepository.AddedEntries);
        Assert.Equal(20m, householdAccount.Balance.Amount);
        Assert.Equal(500m, providerBusiness.Balance.Amount);
    }
}

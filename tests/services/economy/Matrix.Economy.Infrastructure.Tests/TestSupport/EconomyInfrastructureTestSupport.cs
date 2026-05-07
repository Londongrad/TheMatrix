using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Tests.TestSupport;

internal static class EconomyInfrastructureTestSupport
{
    internal static EconomyDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<EconomyDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        var dbContext = new EconomyDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    internal static CityBudget CreateBudget(
        Guid cityId,
        string code = "MNY",
        string displayName = "Money",
        string symbol = "$")
    {
        return new CityBudget(
            id: CityBudgetId.New(),
            cityId: cityId,
            unitProfile: new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: code,
                DisplayName: displayName,
                Symbol: symbol));
    }

    internal static CityBudgetLedgerEntry CreateBudgetLedgerEntry(
        Guid cityId,
        Guid? entryId = null,
        DateTimeOffset? occurredAtUtc = null,
        CityBudgetLedgerEntryKind kind = CityBudgetLedgerEntryKind.Revenue,
        CityBudgetCategory category = CityBudgetCategory.General,
        decimal amount = 10m,
        CityBudgetLedgerEntrySource source = CityBudgetLedgerEntrySource.Manual,
        string title = "Entry",
        string? referenceCode = null)
    {
        return new CityBudgetLedgerEntry(
            id: entryId ?? Guid.NewGuid(),
            cityId: cityId,
            occurredAtUtc: occurredAtUtc ?? new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
            kind: kind,
            category: category,
            amount: Money.FromDecimal(amount),
            title: title,
            description: null,
            source: source,
            referenceCode: referenceCode);
    }

    internal static CityBusiness CreateBusiness(
        Guid cityId,
        string name,
        string externalReferenceCode,
        string templateKey,
        CityBusinessKind kind = CityBusinessKind.RetailStore)
    {
        return new CityBusiness(
            id: Guid.NewGuid(),
            cityId: cityId,
            name: name,
            externalReferenceCode: externalReferenceCode,
            templateKey: templateKey,
            kind: kind,
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            initialCapital: Money.FromDecimal(100m));
    }

    internal static CityHouseholdAccount CreateHouseholdAccount(
        Guid cityId,
        string name,
        string externalReferenceCode)
    {
        return new CityHouseholdAccount(
            id: Guid.NewGuid(),
            cityId: cityId,
            name: name,
            externalReferenceCode: externalReferenceCode,
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            openingBalance: Money.FromDecimal(100m));
    }

    internal static CityHouseholdObligation CreateHouseholdObligation(
        Guid cityId,
        Guid householdAccountId,
        Guid providerBusinessId,
        string name,
        DateTimeOffset? firstChargeDueAtUtc = null)
    {
        return new CityHouseholdObligation(
            id: Guid.NewGuid(),
            cityId: cityId,
            householdAccountId: householdAccountId,
            providerBusinessId: providerBusinessId,
            name: name,
            kind: CityHouseholdObligationKind.Utilities,
            billingCadence: CityHouseholdObligationBillingCadence.Monthly,
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            firstChargeDueAtUtc: firstChargeDueAtUtc ?? new DateTimeOffset(2048, 5, 7, 8, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            chargeAmount: Money.FromDecimal(40m),
            taxAmount: Money.FromDecimal(4m));
    }
}

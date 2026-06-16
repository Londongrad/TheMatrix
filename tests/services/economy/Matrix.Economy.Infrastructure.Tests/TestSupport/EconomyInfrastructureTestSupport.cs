using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.ValueObjects;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Tests.TestSupport
{
    internal static class EconomyInfrastructureTestSupport
    {
        internal static EconomyDbContext CreateDbContext(string? databaseName = null)
        {
            DbContextOptions<EconomyDbContext> options = new DbContextOptionsBuilder<EconomyDbContext>()
               .UseInMemoryDatabase(
                    databaseName ??
                    Guid.NewGuid()
                       .ToString("N"))
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
                occurredAtUtc: occurredAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: kind,
                category: category,
                amount: Money.FromDecimal(amount),
                title: title,
                description: null,
                source: source,
                referenceCode: referenceCode);
        }

        internal static CityBudgetAllocation CreateBudgetAllocation(
            Guid cityId,
            CityBudgetCategory category,
            decimal targetAmount)
        {
            return new CityBudgetAllocation(
                id: Guid.NewGuid(),
                cityId: cityId,
                category: category,
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                unitProfile: CityBudgetUnitProfile.DefaultMoney(),
                targetAmount: Money.FromDecimal(targetAmount));
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
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                unitProfile: CityBudgetUnitProfile.DefaultMoney(),
                initialCapital: Money.FromDecimal(100m));
        }

        internal static CityBusinessLedgerEntry CreateBusinessLedgerEntry(
            Guid businessId,
            Guid cityId,
            Guid? entryId = null,
            DateTimeOffset? occurredAtUtc = null,
            CityBusinessLedgerEntryKind kind = CityBusinessLedgerEntryKind.RetailSale,
            decimal amount = 10m,
            decimal taxAmount = 1m,
            string title = "Entry",
            string? referenceCode = null)
        {
            return new CityBusinessLedgerEntry(
                id: entryId ?? Guid.NewGuid(),
                businessId: businessId,
                cityId: cityId,
                occurredAtUtc: occurredAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: kind,
                amount: Money.FromDecimal(amount),
                taxAmount: Money.FromDecimal(taxAmount),
                title: title,
                description: null,
                source: CityBusinessLedgerEntrySource.Manual,
                referenceCode: referenceCode);
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
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                unitProfile: CityBudgetUnitProfile.DefaultMoney(),
                openingBalance: Money.FromDecimal(100m));
        }

        internal static CityHouseholdAccountLedgerEntry CreateHouseholdAccountLedgerEntry(
            Guid householdAccountId,
            Guid cityId,
            Guid? entryId = null,
            DateTimeOffset? occurredAtUtc = null,
            CityHouseholdAccountLedgerEntryKind kind = CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
            decimal amount = 10m,
            string title = "Entry",
            string? referenceCode = null)
        {
            return new CityHouseholdAccountLedgerEntry(
                id: entryId ?? Guid.NewGuid(),
                householdAccountId: householdAccountId,
                cityId: cityId,
                occurredAtUtc: occurredAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: kind,
                amount: Money.FromDecimal(amount),
                title: title,
                description: null,
                source: CityHouseholdAccountLedgerEntrySource.Manual,
                referenceCode: referenceCode);
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
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                firstChargeDueAtUtc: firstChargeDueAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                unitProfile: CityBudgetUnitProfile.DefaultMoney(),
                chargeAmount: Money.FromDecimal(40m),
                taxAmount: Money.FromDecimal(4m));
        }

        internal static CityBudgetSettlement CreateBudgetSettlement(
            Guid cityId,
            long tickId,
            string correlationId)
        {
            return new CityBudgetSettlement(
                id: Guid.NewGuid(),
                cityId: cityId,
                tickId: tickId,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                settledDays: 1,
                householdCount: 10,
                residentCount: 30,
                grossPayroll: Money.FromDecimal(100m),
                incomeTax: Money.FromDecimal(10m),
                netPayroll: Money.FromDecimal(90m),
                retailTurnover: Money.FromDecimal(50m),
                retailTax: Money.FromDecimal(5m),
                housingSpend: Money.FromDecimal(20m),
                correlationId: correlationId,
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        internal static CityEconomyCostProfileState CreateCostProfileState(Guid cityId)
        {
            return CityEconomyCostProfileState.Create(
                cityId: cityId,
                seed: new CityEconomyCostProfileSnapshot(
                    WageMultiplier: 1.1m,
                    RetailPriceMultiplier: 1.2m,
                    HousingCostMultiplier: 1.3m,
                    UtilityCostMultiplier: 1.4m,
                    CostOfLivingIndex: 1.25m,
                    AffordabilityIndex: 0.9m,
                    EvaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 11,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        internal static CityEconomyProgressionState CreateProgressionState(Guid cityId)
        {
            return CityEconomyProgressionState.Create(
                cityId: cityId,
                lastCompletedTickId: 12,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}

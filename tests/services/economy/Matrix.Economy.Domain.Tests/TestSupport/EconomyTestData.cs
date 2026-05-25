using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Tests.TestSupport
{
    internal static class EconomyTestData
    {
        internal static readonly DateTimeOffset DefaultCreatedAtUtc = new(
            year: 2048,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);

        internal static CityBudgetUnitProfile CreateUnitProfile(
            string code = "CR",
            string displayName = "Credits",
            string symbol = "$")
        {
            return new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: code,
                DisplayName: displayName,
                Symbol: symbol);
        }

        internal static CityBudget CreateBudget(
            Guid cityId,
            decimal balance = 0m)
        {
            var budget = new CityBudget(
                id: new CityBudgetId(Guid.Parse("10000000-0000-0000-0000-000000000001")),
                cityId: cityId,
                unitProfile: CreateUnitProfile());

            if (balance > 0m)
                budget.ApplyLedgerEntry(
                    new CityBudgetLedgerEntry(
                        id: Guid.Parse("10000000-0000-0000-0000-000000000101"),
                        cityId: cityId,
                        occurredAtUtc: DefaultCreatedAtUtc,
                        kind: CityBudgetLedgerEntryKind.Revenue,
                        category: CityBudgetCategory.General,
                        amount: Money.FromDecimal(balance),
                        title: "Budget seed revenue",
                        description: "Budget seed revenue",
                        source: CityBudgetLedgerEntrySource.MunicipalOperations,
                        referenceCode: "seed"));
            else
                if (balance < 0m)
                budget.ApplyLedgerEntry(
                    new CityBudgetLedgerEntry(
                        id: Guid.Parse("10000000-0000-0000-0000-000000000102"),
                        cityId: cityId,
                        occurredAtUtc: DefaultCreatedAtUtc,
                        kind: CityBudgetLedgerEntryKind.Expense,
                        category: CityBudgetCategory.General,
                        amount: Money.FromDecimal(Math.Abs(balance)),
                        title: "Budget seed expense",
                        description: "Budget seed expense",
                        source: CityBudgetLedgerEntrySource.MunicipalOperations,
                        referenceCode: "seed"));

            return budget;
        }

        internal static CityBudgetAllocation CreateAllocation(
            Guid cityId,
            CityBudgetCategory category,
            decimal targetAmount,
            decimal totalSpent = 0m)
        {
            var allocation = new CityBudgetAllocation(
                id: Guid.NewGuid(),
                cityId: cityId,
                category: category,
                createdAtUtc: DefaultCreatedAtUtc,
                unitProfile: CreateUnitProfile(),
                targetAmount: Money.FromDecimal(targetAmount));

            if (totalSpent > 0m)
                allocation.RecordExpense(
                    amount: Money.FromDecimal(totalSpent),
                    updatedAtUtc: DefaultCreatedAtUtc.AddDays(1));

            return allocation;
        }

        internal static CityBusiness CreateBusiness(
            Guid cityId,
            CityBusinessKind kind,
            string name = "Business",
            decimal initialCapital = 1000m)
        {
            return new CityBusiness(
                id: Guid.NewGuid(),
                cityId: cityId,
                name: name,
                externalReferenceCode: "external-ref",
                templateKey: "template-key",
                kind: kind,
                createdAtUtc: DefaultCreatedAtUtc,
                unitProfile: CreateUnitProfile(),
                initialCapital: Money.FromDecimal(initialCapital));
        }

        internal static CityHouseholdObligation CreateObligation(
            Guid cityId,
            CityHouseholdObligationKind kind = CityHouseholdObligationKind.Rent,
            CityHouseholdObligationBillingCadence cadence = CityHouseholdObligationBillingCadence.Monthly,
            DateTimeOffset? firstChargeDueAtUtc = null,
            decimal chargeAmount = 125m,
            decimal taxAmount = 25m)
        {
            return new CityHouseholdObligation(
                id: Guid.Parse("10000000-0000-0000-0000-000000000201"),
                cityId: cityId,
                householdAccountId: Guid.Parse("10000000-0000-0000-0000-000000000202"),
                providerBusinessId: Guid.Parse("10000000-0000-0000-0000-000000000203"),
                name: " Household obligation ",
                kind: kind,
                billingCadence: cadence,
                createdAtUtc: DefaultCreatedAtUtc,
                firstChargeDueAtUtc: firstChargeDueAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 10,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                unitProfile: CreateUnitProfile(),
                chargeAmount: Money.FromDecimal(chargeAmount),
                taxAmount: Money.FromDecimal(taxAmount));
        }
    }
}

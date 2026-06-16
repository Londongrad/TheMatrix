using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common
{
    public sealed class CityBudgetOperationalExpenseSupport(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        CityBudgetAllocationExpenseSupport allocationExpenseSupport)
    {
        public async Task<BudgetLedgerEntryDto?> RecordAsync(
            Guid cityId,
            CityBudgetCategory category,
            decimal amount,
            string title,
            string? description,
            DateTimeOffset occurredAtUtc,
            string referenceCode,
            CancellationToken cancellationToken)
        {
            if (await budgetLedgerRepository.ExistsAsync(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Expense,
                    referenceCode: referenceCode,
                    cancellationToken: cancellationToken))
                return null;

            var unitProfile = CityBudgetUnitProfile.DefaultMoney();
            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: cityId,
                                    cancellationToken: cancellationToken) ??
                                CreateBudget(
                                    cityId: cityId,
                                    budgetRepository: budgetRepository,
                                    unitProfile: unitProfile);
            budget.EnsureCompatibleUnit(unitProfile);

            var entry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: cityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBudgetLedgerEntryKind.Expense,
                category: category,
                amount: Money.FromDecimal(amount),
                title: title,
                description: description,
                source: CityBudgetLedgerEntrySource.MunicipalOperations,
                referenceCode: referenceCode);

            budget.ApplyLedgerEntry(entry);
            await allocationExpenseSupport.RecordExpenseAsync(
                cityId: cityId,
                category: category,
                amount: entry.Amount,
                unitProfile: budget.GetUnitProfile(),
                cancellationToken: cancellationToken);
            await budgetLedgerRepository.AddAsync(
                entry: entry,
                cancellationToken: cancellationToken);

            return new BudgetLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                UnitKind: budget.UnitKind.ToString(),
                UnitCode: budget.UnitCode,
                UnitDisplayName: budget.UnitDisplayName,
                UnitSymbol: budget.UnitSymbol,
                Kind: entry.Kind.ToString(),
                Category: entry.Category.ToString(),
                Amount: entry.Amount.Amount,
                Title: entry.Title,
                Description: entry.Description,
                Source: entry.Source.ToString(),
                ReferenceCode: entry.ReferenceCode);
        }

        private static CityBudget CreateBudget(
            Guid cityId,
            ICityBudgetRepository budgetRepository,
            CityBudgetUnitProfile unitProfile)
        {
            var budget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: cityId,
                unitProfile: unitProfile);
            budgetRepository.Add(budget);
            return budget;
        }
    }
}

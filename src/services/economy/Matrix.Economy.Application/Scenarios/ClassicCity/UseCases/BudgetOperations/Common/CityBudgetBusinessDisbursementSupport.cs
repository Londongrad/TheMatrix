using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common
{
    public sealed class CityBudgetBusinessDisbursementSupport(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        CityBudgetAllocationExpenseSupport allocationExpenseSupport,
        TimeProvider timeProvider)
    {
        public async Task<BudgetLedgerEntryDto> DisburseAsync(
            CityBusiness business,
            CityBudgetCategory category,
            decimal amount,
            string title,
            string? description,
            CancellationToken cancellationToken)
        {
            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: business.CityId,
                                    cancellationToken: cancellationToken) ??
                                CreateBudget(
                                    cityId: business.CityId,
                                    unitProfile: business.GetUnitProfile(),
                                    budgetRepository: budgetRepository);
            budget.EnsureCompatibleUnit(business.GetUnitProfile());

            var moneyAmount = Money.FromDecimal(amount);
            business.RecordMunicipalRevenue(moneyAmount);

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();

            var budgetEntry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBudgetLedgerEntryKind.Expense,
                category: category,
                amount: moneyAmount,
                title: title,
                description: description,
                source: CityBudgetLedgerEntrySource.MunicipalDisbursement,
                referenceCode: business.Id.ToString("N"));

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBusinessLedgerEntryKind.MunicipalRevenue,
                amount: moneyAmount,
                taxAmount: Money.Zero,
                title: title,
                description: description,
                source: CityBusinessLedgerEntrySource.MunicipalBudget,
                referenceCode: budget.CityId.ToString("N"));

            budget.ApplyLedgerEntry(budgetEntry);
            await allocationExpenseSupport.RecordExpenseAsync(
                cityId: business.CityId,
                category: category,
                amount: budgetEntry.Amount,
                unitProfile: budget.GetUnitProfile(),
                cancellationToken: cancellationToken);
            await budgetLedgerRepository.AddAsync(
                entry: budgetEntry,
                cancellationToken: cancellationToken);
            await businessLedgerRepository.AddAsync(
                entry: businessEntry,
                cancellationToken: cancellationToken);

            return new BudgetLedgerEntryDto(
                EntryId: budgetEntry.Id,
                OccurredAtUtc: budgetEntry.OccurredAtUtc.ToString("O"),
                UnitKind: budget.UnitKind.ToString(),
                UnitCode: budget.UnitCode,
                UnitDisplayName: budget.UnitDisplayName,
                UnitSymbol: budget.UnitSymbol,
                Kind: budgetEntry.Kind.ToString(),
                Category: budgetEntry.Category.ToString(),
                Amount: budgetEntry.Amount.Amount,
                Title: budgetEntry.Title,
                Description: budgetEntry.Description,
                Source: budgetEntry.Source.ToString(),
                ReferenceCode: budgetEntry.ReferenceCode);
        }

        private static CityBudget CreateBudget(
            Guid cityId,
            CityBudgetUnitProfile unitProfile,
            ICityBudgetRepository budgetRepository)
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

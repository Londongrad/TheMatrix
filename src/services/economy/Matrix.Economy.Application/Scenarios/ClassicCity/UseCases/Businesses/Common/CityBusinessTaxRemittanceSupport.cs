using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.ValueObjects;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common
{
    public sealed class CityBusinessTaxRemittanceSupport(
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        TimeProvider timeProvider)
    {
        public async Task<CityBusinessLedgerEntryDto> RemitAsync(
            CityBusiness business,
            Money amount,
            CityBudgetCategory budgetCategory,
            string title,
            string? description,
            CancellationToken cancellationToken)
        {
            business.RemitTax(amount);

            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: business.CityId,
                                    cancellationToken: cancellationToken) ??
                                CreateBudget(
                                    business: business,
                                    budgetRepository: budgetRepository);
            budget.EnsureCompatibleUnit(business.GetUnitProfile());

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBusinessLedgerEntryKind.TaxRemittance,
                amount: amount,
                taxAmount: amount,
                title: title,
                description: description,
                source: CityBusinessLedgerEntrySource.TaxRemittance,
                referenceCode: budget.CityId.ToString("N"));

            var budgetEntry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: budgetCategory,
                amount: amount,
                title: title,
                description: $"Business remittance from {business.Name}. {description}".Trim(),
                source: CityBudgetLedgerEntrySource.BusinessRemittance,
                referenceCode: business.Id.ToString("N"));

            budget.ApplyLedgerEntry(budgetEntry);
            await businessLedgerRepository.AddAsync(
                entry: businessEntry,
                cancellationToken: cancellationToken);
            await budgetLedgerRepository.AddAsync(
                entry: budgetEntry,
                cancellationToken: cancellationToken);

            return new CityBusinessLedgerEntryDto(
                EntryId: businessEntry.Id,
                OccurredAtUtc: businessEntry.OccurredAtUtc.ToString("O"),
                UnitKind: business.UnitKind.ToString(),
                UnitCode: business.UnitCode,
                UnitDisplayName: business.UnitDisplayName,
                UnitSymbol: business.UnitSymbol,
                Kind: businessEntry.Kind.ToString(),
                Amount: businessEntry.Amount.Amount,
                TaxAmount: businessEntry.TaxAmount.Amount,
                Title: businessEntry.Title,
                Description: businessEntry.Description,
                Source: businessEntry.Source.ToString(),
                ReferenceCode: businessEntry.ReferenceCode);
        }

        private static CityBudget CreateBudget(
            CityBusiness business,
            ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: business.CityId,
                unitProfile: business.GetUnitProfile());
            budgetRepository.Add(budget);
            return budget;
        }
    }
}

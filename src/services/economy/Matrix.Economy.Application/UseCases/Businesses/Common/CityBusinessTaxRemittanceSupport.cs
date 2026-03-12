using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;

namespace Matrix.Economy.Application.UseCases.Businesses.Common
{
    public sealed class CityBusinessTaxRemittanceSupport(
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository)
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

            CityBudget budget = await budgetRepository.GetByCityAsync(business.CityId, cancellationToken)
                ?? CreateBudget(business, budgetRepository);
            budget.EnsureCompatibleUnit(business.GetUnitProfile());

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;

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
            await businessLedgerRepository.AddAsync(businessEntry, cancellationToken);
            await budgetLedgerRepository.AddAsync(budgetEntry, cancellationToken);

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

        private static CityBudget CreateBudget(CityBusiness business, ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), business.CityId, business.GetUnitProfile());
            budgetRepository.Add(budget);
            return budget;
        }
    }
}

using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RemitCityBusinessTax
{
    public sealed class RemitCityBusinessTaxCommandHandler(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RemitCityBusinessTaxCommand, CityBusinessLedgerEntryDto>
    {
        public async Task<CityBusinessLedgerEntryDto> Handle(
            RemitCityBusinessTaxCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.BusinessId}' was not found.");

            Money amount = Money.FromDecimal(request.Amount);
            business.RemitTax(amount);

            CityBudget budget = await budgetRepository.GetByCityAsync(business.CityId, cancellationToken)
                ?? CreateBudget(business, budgetRepository);
            budget.EnsureCompatibleUnit(business.GetUnitProfile());

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: DateTimeOffset.UtcNow,
                kind: CityBusinessLedgerEntryKind.TaxRemittance,
                amount: amount,
                taxAmount: amount,
                title: request.Title,
                description: request.Description,
                source: CityBusinessLedgerEntrySource.TaxRemittance,
                referenceCode: budget.CityId.ToString("N"));

            var budgetEntry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: business.CityId,
                occurredAtUtc: businessEntry.OccurredAtUtc,
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: request.BudgetCategory,
                amount: amount,
                title: request.Title,
                description: $"Business remittance from {business.Name}. {request.Description}".Trim(),
                source: CityBudgetLedgerEntrySource.BusinessRemittance,
                referenceCode: business.Id.ToString("N"));

            budget.ApplyLedgerEntry(budgetEntry);
            await businessLedgerRepository.AddAsync(businessEntry, cancellationToken);
            await budgetLedgerRepository.AddAsync(budgetEntry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(businessEntry, business.GetUnitProfile());
        }

        private static CityBudget CreateBudget(CityBusiness business, ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), business.CityId, business.GetUnitProfile());
            budgetRepository.Add(budget);
            return budget;
        }

        private static CityBusinessLedgerEntryDto Map(
            CityBusinessLedgerEntry entry,
            CityBudgetUnitProfile unitProfile)
        {
            return new CityBusinessLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                UnitKind: unitProfile.Kind.ToString(),
                UnitCode: unitProfile.Code,
                UnitDisplayName: unitProfile.DisplayName,
                UnitSymbol: unitProfile.Symbol,
                Kind: entry.Kind.ToString(),
                Amount: entry.Amount.Amount,
                TaxAmount: entry.TaxAmount.Amount,
                Title: entry.Title,
                Description: entry.Description,
                Source: entry.Source.ToString(),
                ReferenceCode: entry.ReferenceCode);
        }
    }
}

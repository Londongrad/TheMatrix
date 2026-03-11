using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessRetailSale
{
    public sealed class RecordCityBusinessRetailSaleCommandHandler(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository ledgerRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RecordCityBusinessRetailSaleCommand, CityBusinessLedgerEntryDto>
    {
        public async Task<CityBusinessLedgerEntryDto> Handle(
            RecordCityBusinessRetailSaleCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.BusinessId}' was not found.");

            Money grossAmount = Money.FromDecimal(request.GrossAmount);
            Money salesTaxAmount = Money.FromDecimal(request.SalesTaxAmount);
            business.RecordRetailSale(grossAmount, salesTaxAmount);

            var entry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: DateTimeOffset.UtcNow,
                kind: CityBusinessLedgerEntryKind.RetailSale,
                amount: grossAmount,
                taxAmount: salesTaxAmount,
                title: request.Title,
                description: request.Description,
                source: CityBusinessLedgerEntrySource.RetailSale,
                referenceCode: null);

            await ledgerRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(entry, business.GetUnitProfile());
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

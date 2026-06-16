using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessRetailSale
{
    public sealed class RecordCityBusinessRetailSaleCommandHandler(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository ledgerRepository,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<RecordCityBusinessRetailSaleCommand, CityBusinessLedgerEntryDto>
    {
        public async Task<CityBusinessLedgerEntryDto> Handle(
            RecordCityBusinessRetailSaleCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new InvalidOperationException(
                                        $"Business '{request.BusinessId}' was not found.");

            var grossAmount = Money.FromDecimal(request.GrossAmount);
            var salesTaxAmount = Money.FromDecimal(request.SalesTaxAmount);
            business.RecordRetailSale(
                grossAmount: grossAmount,
                salesTaxAmount: salesTaxAmount);

            var entry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: timeProvider.GetUtcNow(),
                kind: CityBusinessLedgerEntryKind.RetailSale,
                amount: grossAmount,
                taxAmount: salesTaxAmount,
                title: request.Title,
                description: request.Description,
                source: CityBusinessLedgerEntrySource.RetailSale,
                referenceCode: null);

            await ledgerRepository.AddAsync(
                entry: entry,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(
                entry: entry,
                unitProfile: business.GetUnitProfile());
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

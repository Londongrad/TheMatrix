using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase
{
    public sealed class RecordCityHouseholdPurchaseCommandHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<RecordCityHouseholdPurchaseCommand, CityHouseholdAccountLedgerEntryDto>
    {
        public async Task<CityHouseholdAccountLedgerEntryDto> Handle(
            RecordCityHouseholdPurchaseCommand request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccount householdAccount =
                await householdAccountRepository.GetByIdAsync(
                    householdAccountId: request.HouseholdAccountId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException($"Household account '{request.HouseholdAccountId}' was not found.");
            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new InvalidOperationException(
                                        $"Business '{request.BusinessId}' was not found.");

            business.EnsureCanRecordConsumerSale();

            if (business.CityId != householdAccount.CityId)
                throw new InvalidOperationException("Household account and business must belong to the same city.");

            business.EnsureCompatibleUnit(householdAccount.GetUnitProfile());

            var grossAmount = Money.FromDecimal(request.GrossAmount);
            var salesTaxAmount = Money.FromDecimal(request.SalesTaxAmount);

            householdAccount.RecordConsumerPurchase(grossAmount);
            business.RecordRetailSale(
                grossAmount: grossAmount,
                salesTaxAmount: salesTaxAmount);

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();

            var householdEntry = new CityHouseholdAccountLedgerEntry(
                id: Guid.NewGuid(),
                householdAccountId: householdAccount.Id,
                cityId: householdAccount.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                amount: grossAmount,
                title: request.Title,
                description: request.Description,
                source: CityHouseholdAccountLedgerEntrySource.ConsumerPurchase,
                referenceCode: business.Id.ToString("N"));

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBusinessLedgerEntryKind.RetailSale,
                amount: grossAmount,
                taxAmount: salesTaxAmount,
                title: request.Title,
                description: request.Description,
                source: CityBusinessLedgerEntrySource.RetailSale,
                referenceCode: householdAccount.Id.ToString("N"));

            await householdLedgerRepository.AddAsync(
                entry: householdEntry,
                cancellationToken: cancellationToken);
            await businessLedgerRepository.AddAsync(
                entry: businessEntry,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(
                entry: householdEntry,
                unitProfile: householdAccount.GetUnitProfile());
        }

        private static CityHouseholdAccountLedgerEntryDto Map(
            CityHouseholdAccountLedgerEntry entry,
            CityBudgetUnitProfile unitProfile)
        {
            return new CityHouseholdAccountLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                UnitKind: unitProfile.Kind.ToString(),
                UnitCode: unitProfile.Code,
                UnitDisplayName: unitProfile.DisplayName,
                UnitSymbol: unitProfile.Symbol,
                Kind: entry.Kind.ToString(),
                Amount: entry.Amount.Amount,
                Title: entry.Title,
                Description: entry.Description,
                Source: entry.Source.ToString(),
                ReferenceCode: entry.ReferenceCode);
        }
    }
}

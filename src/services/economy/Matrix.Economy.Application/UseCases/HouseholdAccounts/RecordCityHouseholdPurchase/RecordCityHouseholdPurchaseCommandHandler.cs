using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase
{
    public sealed class RecordCityHouseholdPurchaseCommandHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RecordCityHouseholdPurchaseCommand, CityHouseholdAccountLedgerEntryDto>
    {
        public async Task<CityHouseholdAccountLedgerEntryDto> Handle(
            RecordCityHouseholdPurchaseCommand request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccount householdAccount = await householdAccountRepository.GetByIdAsync(request.HouseholdAccountId, cancellationToken)
                ?? throw new InvalidOperationException($"Household account '{request.HouseholdAccountId}' was not found.");
            CityBusiness business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.BusinessId}' was not found.");

            business.EnsureCanRecordConsumerSale();

            if (business.CityId != householdAccount.CityId)
            {
                throw new InvalidOperationException("Household account and business must belong to the same city.");
            }

            business.EnsureCompatibleUnit(householdAccount.GetUnitProfile());

            Money grossAmount = Money.FromDecimal(request.GrossAmount);
            Money salesTaxAmount = Money.FromDecimal(request.SalesTaxAmount);

            householdAccount.RecordConsumerPurchase(grossAmount);
            business.RecordRetailSale(grossAmount, salesTaxAmount);

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;

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

            await householdLedgerRepository.AddAsync(householdEntry, cancellationToken);
            await businessLedgerRepository.AddAsync(businessEntry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(householdEntry, householdAccount.GetUnitProfile());
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

using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.Common
{
    public sealed class HouseholdObligationChargeSupport(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository)
    {
        public async Task<CityHouseholdAccountLedgerEntryDto> ChargeAsync(
            CityHouseholdObligation obligation,
            string? description,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccount householdAccount = await householdAccountRepository.GetByIdAsync(obligation.HouseholdAccountId, cancellationToken)
                ?? throw new InvalidOperationException($"Household account '{obligation.HouseholdAccountId}' was not found.");
            CityBusiness providerBusiness = await businessRepository.GetByIdAsync(obligation.ProviderBusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{obligation.ProviderBusinessId}' was not found.");

            if (householdAccount.CityId != obligation.CityId || providerBusiness.CityId != obligation.CityId)
            {
                throw new InvalidOperationException("Obligation actors must belong to the same city.");
            }

            householdAccount.EnsureCompatibleUnit(obligation.GetUnitProfile());
            providerBusiness.EnsureCompatibleUnit(obligation.GetUnitProfile());

            householdAccount.RecordObligationCharge(obligation.ChargeAmount);
            providerBusiness.RecordObligationRevenue(obligation.ChargeAmount, obligation.TaxAmount);

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;
            obligation.MarkCharged(occurredAtUtc);

            string title = obligation.Kind switch
            {
                CityHouseholdObligationKind.Rent => $"{obligation.Name} rent charge",
                CityHouseholdObligationKind.Utilities => $"{obligation.Name} utility charge",
                _ => $"{obligation.Name} obligation charge"
            };

            string normalizedDescription = description?.Trim() ?? string.Empty;

            var householdEntry = new CityHouseholdAccountLedgerEntry(
                id: Guid.NewGuid(),
                householdAccountId: householdAccount.Id,
                cityId: householdAccount.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityHouseholdAccountLedgerEntryKind.ObligationCharge,
                amount: obligation.ChargeAmount,
                title: title,
                description: normalizedDescription,
                source: CityHouseholdAccountLedgerEntrySource.Obligation,
                referenceCode: obligation.Id.ToString("N"));

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: providerBusiness.Id,
                cityId: providerBusiness.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBusinessLedgerEntryKind.ObligationRevenue,
                amount: obligation.ChargeAmount,
                taxAmount: obligation.TaxAmount,
                title: title,
                description: normalizedDescription,
                source: CityBusinessLedgerEntrySource.Obligation,
                referenceCode: obligation.Id.ToString("N"));

            await householdLedgerRepository.AddAsync(householdEntry, cancellationToken);
            await businessLedgerRepository.AddAsync(businessEntry, cancellationToken);

            return new CityHouseholdAccountLedgerEntryDto(
                EntryId: householdEntry.Id,
                OccurredAtUtc: householdEntry.OccurredAtUtc.ToString("O"),
                UnitKind: householdAccount.UnitKind.ToString(),
                UnitCode: householdAccount.UnitCode,
                UnitDisplayName: householdAccount.UnitDisplayName,
                UnitSymbol: householdAccount.UnitSymbol,
                Kind: householdEntry.Kind.ToString(),
                Amount: householdEntry.Amount.Amount,
                Title: householdEntry.Title,
                Description: householdEntry.Description,
                Source: householdEntry.Source.ToString(),
                ReferenceCode: householdEntry.ReferenceCode);
        }
    }
}

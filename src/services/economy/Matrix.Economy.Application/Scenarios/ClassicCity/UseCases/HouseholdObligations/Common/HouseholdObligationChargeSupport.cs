using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common
{
    public sealed class HouseholdObligationChargeSupport(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        TimeProvider timeProvider)
    {
        public async Task<HouseholdObligationChargeAttemptResult> TryChargeAsync(
            CityHouseholdObligation obligation,
            string? description,
            DateTimeOffset? occurredAtUtc,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccount householdAccount =
                await householdAccountRepository.GetByIdAsync(
                    householdAccountId: obligation.HouseholdAccountId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException(
                    $"Household account '{obligation.HouseholdAccountId}' was not found.");
            CityBusiness providerBusiness =
                await businessRepository.GetByIdAsync(
                    businessId: obligation.ProviderBusinessId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException($"Business '{obligation.ProviderBusinessId}' was not found.");

            if (householdAccount.CityId != obligation.CityId || providerBusiness.CityId != obligation.CityId)
                throw new InvalidOperationException("Obligation actors must belong to the same city.");

            householdAccount.EnsureCompatibleUnit(obligation.GetUnitProfile());
            providerBusiness.EnsureCompatibleUnit(obligation.GetUnitProfile());

            DateTimeOffset effectiveOccurredAtUtc = occurredAtUtc?.ToUniversalTime() ?? timeProvider.GetUtcNow();
            Money currentDueAmount = obligation.ResolveCurrentDueAmount(effectiveOccurredAtUtc);
            Money currentDueTaxAmount = obligation.ResolveCurrentDueTaxAmount(effectiveOccurredAtUtc);
            int settledInstallmentCount = obligation.ResolveDueInstallmentCount(effectiveOccurredAtUtc);

            if (!currentDueAmount.IsPositive || settledInstallmentCount <= 0)
                return HouseholdObligationChargeAttemptResult.Failure("NotDue");

            if (currentDueAmount.Amount > householdAccount.Balance.Amount)
            {
                obligation.MarkChargeMissed(effectiveOccurredAtUtc);
                return HouseholdObligationChargeAttemptResult.Failure("InsufficientBalance");
            }

            householdAccount.RecordObligationCharge(currentDueAmount);
            providerBusiness.RecordObligationRevenue(
                grossAmount: currentDueAmount,
                salesTaxAmount: currentDueTaxAmount);
            obligation.MarkCharged(effectiveOccurredAtUtc);

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
                occurredAtUtc: effectiveOccurredAtUtc,
                kind: CityHouseholdAccountLedgerEntryKind.ObligationCharge,
                amount: currentDueAmount,
                title: title,
                description: normalizedDescription,
                source: CityHouseholdAccountLedgerEntrySource.Obligation,
                referenceCode: obligation.Id.ToString("N"));

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: providerBusiness.Id,
                cityId: providerBusiness.CityId,
                occurredAtUtc: effectiveOccurredAtUtc,
                kind: CityBusinessLedgerEntryKind.ObligationRevenue,
                amount: currentDueAmount,
                taxAmount: currentDueTaxAmount,
                title: title,
                description: normalizedDescription,
                source: CityBusinessLedgerEntrySource.Obligation,
                referenceCode: obligation.Id.ToString("N"));

            await householdLedgerRepository.AddAsync(
                entry: householdEntry,
                cancellationToken: cancellationToken);
            await businessLedgerRepository.AddAsync(
                entry: businessEntry,
                cancellationToken: cancellationToken);

            return HouseholdObligationChargeAttemptResult.Success(
                ledgerEntry: new CityHouseholdAccountLedgerEntryDto(
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
                    ReferenceCode: householdEntry.ReferenceCode),
                chargedAmount: currentDueAmount,
                chargedTaxAmount: currentDueTaxAmount,
                settledInstallmentCount: settledInstallmentCount);
        }

        public async Task<CityHouseholdAccountLedgerEntryDto> ChargeAsync(
            CityHouseholdObligation obligation,
            string? description,
            CancellationToken cancellationToken)
        {
            HouseholdObligationChargeAttemptResult result = await TryChargeAsync(
                obligation: obligation,
                description: description,
                occurredAtUtc: null,
                cancellationToken: cancellationToken);

            return result.Succeeded
                ? result.LedgerEntry!
                : throw new InvalidOperationException(
                    $"Could not charge obligation '{obligation.Id}' because '{result.FailureCode}'.");
        }
    }
}

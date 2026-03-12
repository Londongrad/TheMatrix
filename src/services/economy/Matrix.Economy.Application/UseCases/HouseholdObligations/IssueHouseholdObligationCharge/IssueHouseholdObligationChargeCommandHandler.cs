using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge
{
    public sealed class IssueHouseholdObligationChargeCommandHandler(
        ICityHouseholdObligationRepository obligationRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<IssueHouseholdObligationChargeCommand, CityHouseholdAccountLedgerEntryDto>
    {
        public async Task<CityHouseholdAccountLedgerEntryDto> Handle(
            IssueHouseholdObligationChargeCommand request,
            CancellationToken cancellationToken)
        {
            CityHouseholdObligation obligation = await obligationRepository.GetByIdAsync(request.ObligationId, cancellationToken)
                ?? throw new InvalidOperationException($"Obligation '{request.ObligationId}' was not found.");
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

            string description = request.Description?.Trim() ?? string.Empty;

            var householdEntry = new CityHouseholdAccountLedgerEntry(
                id: Guid.NewGuid(),
                householdAccountId: householdAccount.Id,
                cityId: householdAccount.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityHouseholdAccountLedgerEntryKind.ObligationCharge,
                amount: obligation.ChargeAmount,
                title: title,
                description: description,
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
                description: description,
                source: CityBusinessLedgerEntrySource.Obligation,
                referenceCode: obligation.Id.ToString("N"));

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

using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedger
{
    public sealed class GetCityHouseholdAccountLedgerQueryHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository ledgerRepository)
        : IRequestHandler<GetCityHouseholdAccountLedgerQuery, PagedResult<CityHouseholdAccountLedgerEntryDto>>
    {
        public async Task<PagedResult<CityHouseholdAccountLedgerEntryDto>> Handle(
            GetCityHouseholdAccountLedgerQuery request,
            CancellationToken cancellationToken)
        {
            PagedResult<CityHouseholdAccountLedgerEntry> page = await ledgerRepository.GetPageByHouseholdAccountAsync(
                request.HouseholdAccountId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            CityHouseholdAccount account = await householdAccountRepository.GetByIdAsync(request.HouseholdAccountId, cancellationToken)
                ?? throw new InvalidOperationException($"Household account '{request.HouseholdAccountId}' was not found.");
            CityBudgetUnitProfile unitProfile = account.GetUnitProfile();

            return new PagedResult<CityHouseholdAccountLedgerEntryDto>(
                page.Items.Select(entry => Map(entry, unitProfile)).ToArray(),
                page.TotalCount,
                page.PageNumber,
                page.PageSize);
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

using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed
{
    public sealed class GetCityHouseholdAccountLedgerFeedQueryHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository ledgerRepository)
        : IRequestHandler<GetCityHouseholdAccountLedgerFeedQuery, CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>
    {
        public async Task<CursorPagedResult<CityHouseholdAccountLedgerEntryDto>> Handle(
            GetCityHouseholdAccountLedgerFeedQuery request,
            CancellationToken cancellationToken)
        {
            LedgerCursor? cursor = ParseCursor(request.Cursor);

            CursorPagedResult<CityHouseholdAccountLedgerEntry> page =
                await ledgerRepository.GetSliceByHouseholdAccountAsync(
                    householdAccountId: request.HouseholdAccountId,
                    cursor: cursor,
                    pageSize: request.PageSize,
                    cancellationToken: cancellationToken);

            CityHouseholdAccount account =
                await householdAccountRepository.GetByIdAsync(
                    householdAccountId: request.HouseholdAccountId,
                    cancellationToken: cancellationToken) ??
                throw new MatrixApplicationException(
                    code: "Economy.HouseholdAccount.NotFound",
                    message: $"Household account '{request.HouseholdAccountId}' was not found.",
                    errorType: ApplicationErrorType.NotFound);
            CityBudgetUnitProfile unitProfile = account.GetUnitProfile();

            return new CursorPagedResult<CityHouseholdAccountLedgerEntryDto>(
                items: page.Items.Select(entry => Map(
                        entry: entry,
                        unitProfile: unitProfile))
                   .ToArray(),
                pageSize: page.PageSize,
                nextCursor: page.NextCursor);
        }

        private static LedgerCursor? ParseCursor(string? rawCursor)
        {
            if (string.IsNullOrWhiteSpace(rawCursor))
                return null;

            if (LedgerCursorCodec.TryDecode(
                    rawCursor: rawCursor,
                    cursor: out LedgerCursor cursor))
                return cursor;

            throw new MatrixApplicationException(
                code: "Economy.Ledger.InvalidCursor",
                message: "The supplied ledger cursor is invalid.",
                errorType: ApplicationErrorType.Validation);
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

using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger.GetCityBudgetLedgerFeed
{
    public sealed class GetCityBudgetLedgerFeedQueryHandler(
        ICityBudgetLedgerRepository ledgerRepository,
        ICityBudgetRepository budgetRepository)
        : IRequestHandler<GetCityBudgetLedgerFeedQuery, CursorPagedResult<BudgetLedgerEntryDto>>
    {
        public async Task<CursorPagedResult<BudgetLedgerEntryDto>> Handle(
            GetCityBudgetLedgerFeedQuery request,
            CancellationToken cancellationToken)
        {
            LedgerCursor? cursor = ParseCursor(request.Cursor);

            CursorPagedResult<CityBudgetLedgerEntry> page = await ledgerRepository.GetSliceByCityAsync(
                cityId: request.CityId,
                cursor: cursor,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: request.CityId,
                                    cancellationToken: cancellationToken) ??
                                new CityBudget(
                                    id: CityBudgetId.New(),
                                    cityId: request.CityId);
            CityBudgetUnitProfile unitProfile = budget.GetUnitProfile();

            return new CursorPagedResult<BudgetLedgerEntryDto>(
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

        private static BudgetLedgerEntryDto Map(
            CityBudgetLedgerEntry entry,
            CityBudgetUnitProfile unitProfile)
        {
            return new BudgetLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                UnitKind: unitProfile.Kind.ToString(),
                UnitCode: unitProfile.Code,
                UnitDisplayName: unitProfile.DisplayName,
                UnitSymbol: unitProfile.Symbol,
                Kind: entry.Kind.ToString(),
                Category: entry.Category.ToString(),
                Amount: entry.Amount.Amount,
                Title: entry.Title,
                Description: entry.Description,
                Source: entry.Source.ToString(),
                ReferenceCode: entry.ReferenceCode);
        }
    }
}

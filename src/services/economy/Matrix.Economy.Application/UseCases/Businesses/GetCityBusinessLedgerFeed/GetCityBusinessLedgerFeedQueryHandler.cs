using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedgerFeed
{
    public sealed class GetCityBusinessLedgerFeedQueryHandler(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository ledgerRepository)
        : IRequestHandler<GetCityBusinessLedgerFeedQuery, CursorPagedResult<CityBusinessLedgerEntryDto>>
    {
        public async Task<CursorPagedResult<CityBusinessLedgerEntryDto>> Handle(
            GetCityBusinessLedgerFeedQuery request,
            CancellationToken cancellationToken)
        {
            LedgerCursor? cursor = ParseCursor(request.Cursor);

            CursorPagedResult<CityBusinessLedgerEntry> page = await ledgerRepository.GetSliceByBusinessAsync(
                businessId: request.BusinessId,
                cursor: cursor,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new MatrixApplicationException(
                                        code: "Economy.Business.NotFound",
                                        message: $"Business '{request.BusinessId}' was not found.",
                                        errorType: ApplicationErrorType.NotFound);
            CityBudgetUnitProfile unitProfile = business.GetUnitProfile();

            return new CursorPagedResult<CityBusinessLedgerEntryDto>(
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

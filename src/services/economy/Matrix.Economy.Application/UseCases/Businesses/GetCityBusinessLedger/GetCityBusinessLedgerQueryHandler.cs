using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedger
{
    public sealed class GetCityBusinessLedgerQueryHandler(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository ledgerRepository)
        : IRequestHandler<GetCityBusinessLedgerQuery, PagedResult<CityBusinessLedgerEntryDto>>
    {
        public async Task<PagedResult<CityBusinessLedgerEntryDto>> Handle(
            GetCityBusinessLedgerQuery request,
            CancellationToken cancellationToken)
        {
            PagedResult<CityBusinessLedgerEntry> page = await ledgerRepository.GetPageByBusinessAsync(
                businessId: request.BusinessId,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new InvalidOperationException(
                                        $"Business '{request.BusinessId}' was not found.");
            CityBudgetUnitProfile unitProfile = business.GetUnitProfile();

            return new PagedResult<CityBusinessLedgerEntryDto>(
                items: page.Items.Select(entry => Map(
                        entry: entry,
                        unitProfile: unitProfile))
                   .ToArray(),
                totalCount: page.TotalCount,
                pageNumber: page.PageNumber,
                pageSize: page.PageSize);
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

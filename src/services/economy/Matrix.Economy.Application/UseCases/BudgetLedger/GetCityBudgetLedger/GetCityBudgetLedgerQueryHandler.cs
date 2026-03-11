using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedger
{
    public sealed class GetCityBudgetLedgerQueryHandler(ICityBudgetLedgerRepository ledgerRepository)
        : IRequestHandler<GetCityBudgetLedgerQuery, PagedResult<BudgetLedgerEntryDto>>
    {
        public async Task<PagedResult<BudgetLedgerEntryDto>> Handle(
            GetCityBudgetLedgerQuery request,
            CancellationToken cancellationToken)
        {
            PagedResult<CityBudgetLedgerEntry> page = await ledgerRepository.GetPageByCityAsync(
                cityId: request.CityId,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            return new PagedResult<BudgetLedgerEntryDto>(
                items: page.Items.Select(Map).ToArray(),
                totalCount: page.TotalCount,
                pageNumber: page.PageNumber,
                pageSize: page.PageSize);
        }

        private static BudgetLedgerEntryDto Map(CityBudgetLedgerEntry entry)
        {
            return new BudgetLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
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

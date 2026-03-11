using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedger
{
    public sealed class GetCityBudgetLedgerQueryHandler(
        ICityBudgetLedgerRepository ledgerRepository,
        ICityBudgetRepository budgetRepository)
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

            CityBudget budget = await budgetRepository.GetByCityAsync(request.CityId, cancellationToken)
                ?? new CityBudget(CityBudgetId.New(), request.CityId);
            CityBudgetUnitProfile unitProfile = budget.GetUnitProfile();

            return new PagedResult<BudgetLedgerEntryDto>(
                items: page.Items.Select(entry => Map(entry, unitProfile)).ToArray(),
                totalCount: page.TotalCount,
                pageNumber: page.PageNumber,
                pageSize: page.PageSize);
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

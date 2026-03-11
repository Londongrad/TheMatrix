using Matrix.BuildingBlocks.Application.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedger
{
    public sealed record GetCityBudgetLedgerQuery(
        Guid CityId,
        int PageNumber,
        int PageSize) : IRequest<PagedResult<BudgetLedgerEntryDto>>;
}

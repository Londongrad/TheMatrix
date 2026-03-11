using Matrix.BuildingBlocks.Application.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedger
{
    public sealed record GetCityHouseholdAccountLedgerQuery(
        Guid HouseholdAccountId,
        int PageNumber,
        int PageSize) : IRequest<PagedResult<CityHouseholdAccountLedgerEntryDto>>;
}

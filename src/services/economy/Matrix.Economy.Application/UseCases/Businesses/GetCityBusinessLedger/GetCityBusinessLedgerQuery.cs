using Matrix.BuildingBlocks.Application.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedger
{
    public sealed record GetCityBusinessLedgerQuery(
        Guid BusinessId,
        int PageNumber,
        int PageSize) : IRequest<PagedResult<CityBusinessLedgerEntryDto>>;
}

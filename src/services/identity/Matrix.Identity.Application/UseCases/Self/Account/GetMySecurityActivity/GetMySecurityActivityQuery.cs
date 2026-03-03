using Matrix.BuildingBlocks.Application.Models;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed record GetMySecurityActivityQuery(Pagination Pagination)
        : IRequest<PagedResult<SecurityActivityItemResult>>;
}

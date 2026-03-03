using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryHandler(
        ISecurityAuditReadRepository securityAuditReadRepository,
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMySecurityActivityQuery, PagedResult<SecurityActivityItemResult>>
    {
        public async Task<PagedResult<SecurityActivityItemResult>> Handle(
            GetMySecurityActivityQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            (IReadOnlyCollection<SecurityActivityItemResult> items, int totalCount) =
                await securityAuditReadRepository.GetPageByUserIdAsync(
                userId: userId,
                pagination: request.Pagination,
                cancellationToken: cancellationToken);

            return new PagedResult<SecurityActivityItemResult>(
                items: items,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize);
        }
    }
}

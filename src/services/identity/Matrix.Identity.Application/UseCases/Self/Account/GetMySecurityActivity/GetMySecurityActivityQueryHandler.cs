using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryHandler(
        ISecurityAuditReadRepository securityAuditReadRepository,
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMySecurityActivityQuery, IReadOnlyCollection<SecurityActivityItemResult>>
    {
        public Task<IReadOnlyCollection<SecurityActivityItemResult>> Handle(
            GetMySecurityActivityQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            return securityAuditReadRepository.GetRecentByUserIdAsync(
                userId: userId,
                limit: request.Limit,
                cancellationToken: cancellationToken);
        }
    }
}

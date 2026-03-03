using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface ISecurityAuditReadRepository
    {
        Task<(IReadOnlyCollection<SecurityActivityItemResult> Items, int TotalCount)> GetPageByUserIdAsync(
            Guid userId,
            Pagination pagination,
            CancellationToken cancellationToken);
    }
}

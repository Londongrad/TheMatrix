using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface ISecurityAuditReadRepository
    {
        Task<CursorPagedResult<SecurityActivityItemResult>> GetSliceByUserIdAsync(
            Guid userId,
            SecurityActivityCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken);
    }
}

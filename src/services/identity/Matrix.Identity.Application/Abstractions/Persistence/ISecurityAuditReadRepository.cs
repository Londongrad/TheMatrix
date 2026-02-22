using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface ISecurityAuditReadRepository
    {
        Task<IReadOnlyCollection<SecurityActivityItemResult>> GetRecentByUserIdAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken);
    }
}

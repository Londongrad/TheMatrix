using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserSessionRepository
    {
        Task<UserSession?> GetByIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<UserSession?> GetActiveByUserIdAndDeviceIdAsync(
            Guid userId,
            string deviceId,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserSession>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserSession>> ListByUserIdAndDeviceIdAsync(
            Guid userId,
            string deviceId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            UserSession session,
            CancellationToken cancellationToken = default);
    }
}

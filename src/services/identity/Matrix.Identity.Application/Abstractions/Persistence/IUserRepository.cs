using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<User?> GetByIdWithRefreshTokensAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default);

        Task<User?> GetByUsernameAsync(
            string login,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            User user,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            User user,
            CancellationToken cancellationToken = default);

        Task<bool> IsEmailTakenAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default);

        Task<bool> IsUsernameTakenAsync(
            string normalizedUsername,
            CancellationToken cancellationToken = default);

        Task<User?> GetByRefreshTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Returns the permissions version for a user (if exists).
        /// </summary>
        Task<int?> GetPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Returns user IDs that currently have the specified role.
        /// </summary>
        Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken = default);

        Task<bool> BumpPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<int> BumpPermissionsVersionByRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken = default);
    }
}

using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(
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

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task Update(
            User user,
            CancellationToken cancellationToken = default);

        Task<User?> GetByRefreshTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);
    }
}

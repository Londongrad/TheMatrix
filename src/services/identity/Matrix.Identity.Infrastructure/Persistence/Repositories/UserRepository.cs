using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
    {
        private DbSet<User> Users => dbContext.Users;

        public async Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken ct = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.Id == id,
                    cancellationToken: ct);
        }

        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken ct = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.Email.Value == email,
                    cancellationToken: ct);
        }

        public async Task<bool> IsEmailTakenAsync(
            string email,
            CancellationToken ct = default)
        {
            return await Users
               .AnyAsync(
                    predicate: u => u.Email.Value == email,
                    cancellationToken: ct);
        }

        public async Task<User?> GetByUsernameAsync(
            string username,
            CancellationToken ct = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.Username.Value == username,
                    cancellationToken: ct);
        }

        public async Task<bool> IsUsernameTakenAsync(
            string username,
            CancellationToken ct = default)
        {
            return await Users
               .AnyAsync(
                    predicate: u => u.Username.Value == username,
                    cancellationToken: ct);
        }

        public async Task<User?> GetByRefreshTokenHashAsync(
            string tokenHash,
            CancellationToken ct = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash),
                    cancellationToken: ct);
        }

        public async Task AddAsync(
            User user,
            CancellationToken ct = default)
        {
            await Users.AddAsync(
                entity: user,
                cancellationToken: ct);
        }

        public Task DeleteAsync(
            User user,
            CancellationToken ct = default)
        {
            Users.Remove(user);
            return Task.CompletedTask;
        }
    }
}

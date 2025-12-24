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
            CancellationToken cancellationToken = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.Id == id,
                    cancellationToken: cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.Email.Value == email,
                    cancellationToken: cancellationToken);
        }

        public async Task<bool> IsEmailTakenAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await Users
               .AnyAsync(
                    predicate: u => u.Email.Value == email,
                    cancellationToken: cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.Username.Value == username,
                    cancellationToken: cancellationToken);
        }

        public async Task<bool> IsUsernameTakenAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            return await Users
               .AnyAsync(
                    predicate: u => u.Username.Value == username,
                    cancellationToken: cancellationToken);
        }

        public async Task<User?> GetByRefreshTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return await Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                    predicate: u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash),
                    cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            await Users.AddAsync(
                entity: user,
                cancellationToken: cancellationToken);
        }

        public Task DeleteAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            Users.Remove(user);
            return Task.CompletedTask;
        }
    }
}

using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
    {
        private readonly IdentityDbContext _dbContext = dbContext;

        public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    u => u.Email.Value == normalizedEmail,
                    cancellationToken);
        }

        public async Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .AnyAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
               .Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(
                   u => u.Username == normalizedUsername,
                   cancellationToken);
        }

        public async Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .AnyAsync(u => u.Username == normalizedUsername, cancellationToken);
        }

        public async Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash),
                    cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _dbContext.Users.AddAsync(user, cancellationToken);
        }

        public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            _dbContext.Users.Remove(user);
            return Task.CompletedTask;
        }

        public Task Update(User user, CancellationToken cancellationToken = default)
        {
            _dbContext.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

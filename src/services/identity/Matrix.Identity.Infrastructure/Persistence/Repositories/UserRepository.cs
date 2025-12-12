using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
    {
        private readonly IdentityDbContext _dbContext = dbContext;

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    predicate: u => u.Id == id,
                    cancellationToken: ct);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    predicate: u => u.Email.Value == email,
                    cancellationToken: ct);
        }

        public async Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default)
        {
            return await _dbContext.Users
                .AnyAsync(predicate: u => u.Email.Value == email, cancellationToken: ct);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    predicate: u => u.Username.Value == username,
                    cancellationToken: ct);
        }

        public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken ct = default)
        {
            return await _dbContext.Users
                .AnyAsync(predicate: u => u.Username.Value == username, cancellationToken: ct);
        }

        public async Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    predicate: u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash),
                    cancellationToken: ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default) =>
            await _dbContext.Users.AddAsync(entity: user, cancellationToken: ct);

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            _dbContext.Users.Remove(user);
            return Task.CompletedTask;
        }

        public Task Update(User user, CancellationToken ct = default)
        {
            _dbContext.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) => await _dbContext.SaveChangesAsync(ct);
    }
}

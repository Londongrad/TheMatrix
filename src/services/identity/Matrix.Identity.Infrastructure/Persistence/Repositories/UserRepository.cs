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
               .FirstOrDefaultAsync(
                    predicate: u => u.Id == id,
                    cancellationToken: cancellationToken);
        }

        public async Task<User?> GetByIdWithRefreshTokensAsync(
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

        public async Task<bool> ExistsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await Users
               .AsNoTracking()
               .AnyAsync(
                    predicate: u => u.Id == userId,
                    cancellationToken: cancellationToken);
        }

        public async Task<int?> GetPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await Users
               .AsNoTracking()
               .Where(u => u.Id == userId)
               .Select(u => (int?)u.PermissionsVersion)
               .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            // TODO: Potential performance issue for roles with a large number of users
            return await dbContext.UserRoles
               .AsNoTracking()
               .Where(ur => ur.RoleId == roleId)
               .Select(ur => ur.UserId)
               .ToListAsync(cancellationToken);
        }

        public async Task<bool> BumpPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            int affected = await Users
               .Where(u => u.Id == userId)
               .ExecuteUpdateAsync(
                    setPropertyCalls: setters => setters.SetProperty(
                        u => u.PermissionsVersion,
                        u => u.PermissionsVersion + 1),
                    cancellationToken: cancellationToken);

            return affected > 0;
        }

        public async Task<int> BumpPermissionsVersionByRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            int affected = await Users
               .Where(u => dbContext.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId))
               .ExecuteUpdateAsync(
                    setPropertyCalls: setters => setters.SetProperty(
                        u => u.PermissionsVersion,
                        u => u.PermissionsVersion + 1),
                    cancellationToken: cancellationToken);

            return affected;
        }
    }
}

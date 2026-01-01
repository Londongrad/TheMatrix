using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class RoleWriteRepository(IdentityDbContext db) : IRoleWriteRepository
    {
        public async Task AddAsync(
            Role role,
            CancellationToken cancellationToken)
        {
            await db.Roles.AddAsync(
                entity: role,
                cancellationToken: cancellationToken);
        }

        public async Task<Role?> GetByIdForUpdateAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            return await db.Roles.FirstOrDefaultAsync(
                predicate: role => role.Id == roleId,
                cancellationToken: cancellationToken);
        }

        public Task DeleteAsync(
            Role role,
            CancellationToken cancellationToken)
        {
            db.Roles.Remove(role);
            return Task.CompletedTask;
        }
    }
}

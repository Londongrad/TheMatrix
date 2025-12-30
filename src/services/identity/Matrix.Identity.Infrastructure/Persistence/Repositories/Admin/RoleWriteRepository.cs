using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;

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
    }
}

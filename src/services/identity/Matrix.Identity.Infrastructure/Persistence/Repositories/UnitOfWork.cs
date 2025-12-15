using Matrix.BuildingBlocks.Application.Abstractions;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UnitOfWork(IdentityDbContext dbContext) : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

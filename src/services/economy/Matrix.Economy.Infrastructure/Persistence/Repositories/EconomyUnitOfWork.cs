using Matrix.Economy.Application.Abstractions;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class EconomyUnitOfWork(EconomyDbContext dbContext) : IEconomyUnitOfWork
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => _dbContext.SaveChangesAsync(cancellationToken);
    }
}

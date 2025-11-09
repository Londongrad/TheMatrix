using Matrix.CityCore.Application.Abstractions;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityCoreUnitOfWork(CityCoreDbContext dbContext) : ICityCoreUnitOfWork
    {
        private readonly CityCoreDbContext _dbContext = dbContext;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => _dbContext.SaveChangesAsync(cancellationToken);
    }
}

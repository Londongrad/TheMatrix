using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Economy.Application.Abstractions;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class EconomyUnitOfWork(EconomyDbContext dbContext)
        : EfCoreUnitOfWork<EconomyDbContext>(dbContext), IEconomyUnitOfWork;
}

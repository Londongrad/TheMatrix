using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Healthcare.Application.Abstractions;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories
{
    public sealed class HealthcareUnitOfWork(HealthcareDbContext dbContext)
        : EfCoreUnitOfWork<HealthcareDbContext>(dbContext), IHealthcareUnitOfWork;
}

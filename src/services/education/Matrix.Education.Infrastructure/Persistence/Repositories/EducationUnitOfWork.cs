using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Education.Application.Abstractions;

namespace Matrix.Education.Infrastructure.Persistence.Repositories
{
    public sealed class EducationUnitOfWork(EducationDbContext dbContext)
        : EfCoreUnitOfWork<EducationDbContext>(dbContext), IEducationUnitOfWork;
}

using Matrix.Education.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Tests.TestSupport
{
    internal static class EducationInfrastructureTestSupport
    {
        internal static EducationDbContext CreateDbContext(string? databaseName = null)
        {
            DbContextOptions<EducationDbContext> options =
                new DbContextOptionsBuilder<EducationDbContext>()
                   .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
                   .Options;

            var dbContext = new EducationDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }
    }
}

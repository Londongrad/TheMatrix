using Matrix.Healthcare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Tests.TestSupport
{
    internal static class HealthcareInfrastructureTestSupport
    {
        internal static HealthcareDbContext CreateDbContext(string? databaseName = null)
        {
            DbContextOptions<HealthcareDbContext> options =
                new DbContextOptionsBuilder<HealthcareDbContext>()
                   .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
                   .Options;

            var dbContext = new HealthcareDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }
    }
}

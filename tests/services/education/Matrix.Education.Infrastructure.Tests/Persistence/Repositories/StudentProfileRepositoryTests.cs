using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class StudentProfileRepositoryTests
    {
        private static readonly DateTimeOffset SynchronizedAtUtc =
            new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task AddRangeAndGetByIds_PersistsAndLoadsRequestedProfiles()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new StudentProfileRepository(dbContext);
            StudentProfile first = CreateProfile(Guid.NewGuid());
            StudentProfile second = CreateProfile(Guid.NewGuid());
            StudentProfile unrequested = CreateProfile(Guid.NewGuid());

            await repository.AddRangeAsync(new[] { first, second, unrequested });
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<StudentProfile> loaded = await repository.GetByIdsAsync(
                new[] { first.ResidentId, second.ResidentId });

            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, profile => profile.ResidentId == first.ResidentId);
            Assert.Contains(loaded, profile => profile.ResidentId == second.ResidentId);
            Assert.DoesNotContain(loaded, profile => profile.ResidentId == unrequested.ResidentId);
            Assert.All(loaded, profile => Assert.Equal(
                Microsoft.EntityFrameworkCore.EntityState.Unchanged,
                dbContext.Entry(profile).State));
        }

        [Fact]
        public async Task GetByIds_EmptyIds_ReturnsWithoutLoadingProfiles()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new StudentProfileRepository(dbContext);
            dbContext.StudentProfiles.Add(CreateProfile(Guid.NewGuid()));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<StudentProfile> loaded = await repository.GetByIdsAsync(
                Array.Empty<ResidentId>());

            Assert.Empty(loaded);
            Assert.Empty(dbContext.ChangeTracker.Entries<StudentProfile>());
        }

        [Fact]
        public void BatchLookup_TranslatesStrongIdsToPostgreSqlArrayPredicate()
        {
            DbContextOptions<EducationDbContext> options =
                new DbContextOptionsBuilder<EducationDbContext>()
                   .UseNpgsql("Host=localhost;Database=education_translation_test;Username=test;Password=test")
                   .Options;
            using var dbContext = new EducationDbContext(options);
            ResidentId[] residentIds =
            [
                new ResidentId(Guid.NewGuid()),
                new ResidentId(Guid.NewGuid())
            ];

            string sql = dbContext.StudentProfiles
               .Where(profile => residentIds.Contains(profile.Id))
               .ToQueryString();

            Assert.Contains("resident_id", sql, StringComparison.Ordinal);
            Assert.Contains("ANY", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static StudentProfile CreateProfile(Guid residentId)
        {
            return StudentProfile.Register(
                residentId: new ResidentId(residentId),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: new DateOnly(2030, 5, 12),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: SynchronizedAtUtc);
        }
    }
}

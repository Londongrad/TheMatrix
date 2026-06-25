using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class PatientProfileRepositoryTests
    {
        private static readonly DateTimeOffset SynchronizedAtUtc =
            new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task AddRangeAndGetByIds_PersistsAndLoadsRequestedProfiles()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientProfileRepository(dbContext);
            PatientProfile first = CreateProfile(Guid.NewGuid());
            PatientProfile second = CreateProfile(Guid.NewGuid());
            PatientProfile unrequested = CreateProfile(Guid.NewGuid());

            await repository.AddRangeAsync(new[] { first, second, unrequested });
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<PatientProfile> loaded = await repository.GetByIdsAsync(
                new[] { first.PatientId, second.PatientId });

            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, profile => profile.PatientId == first.PatientId);
            Assert.Contains(loaded, profile => profile.PatientId == second.PatientId);
            Assert.DoesNotContain(loaded, profile => profile.PatientId == unrequested.PatientId);
            Assert.All(loaded, profile => Assert.Equal(
                EntityState.Unchanged,
                dbContext.Entry(profile).State));
        }

        [Fact]
        public async Task GetByIds_EmptyIds_ReturnsWithoutLoadingProfiles()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientProfileRepository(dbContext);
            dbContext.PatientProfiles.Add(CreateProfile(Guid.NewGuid()));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<PatientProfile> loaded = await repository.GetByIdsAsync(
                Array.Empty<PatientId>());

            Assert.Empty(loaded);
            Assert.Empty(dbContext.ChangeTracker.Entries<PatientProfile>());
        }

        [Fact]
        public void BatchLookup_TranslatesStrongIdsToPostgreSqlArrayPredicate()
        {
            DbContextOptions<HealthcareDbContext> options =
                new DbContextOptionsBuilder<HealthcareDbContext>()
                   .UseNpgsql("Host=localhost;Database=healthcare_translation_test;Username=test;Password=test")
                   .Options;
            using var dbContext = new HealthcareDbContext(options);
            PatientId[] patientIds =
            [
                new PatientId(Guid.NewGuid()),
                new PatientId(Guid.NewGuid())
            ];

            string sql = dbContext.PatientProfiles
               .Where(profile => patientIds.Contains(profile.Id))
               .ToQueryString();

            Assert.Contains("patient_id", sql, StringComparison.Ordinal);
            Assert.Contains("ANY", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static PatientProfile CreateProfile(Guid patientId)
        {
            return PatientProfile.Register(
                patientId: new PatientId(patientId),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: new DateOnly(2030, 5, 12),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: SynchronizedAtUtc);
        }
    }
}

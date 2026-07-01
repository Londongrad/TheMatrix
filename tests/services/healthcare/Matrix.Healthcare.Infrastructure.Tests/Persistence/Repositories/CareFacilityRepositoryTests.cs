using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CareFacilityRepositoryTests
    {
        private static readonly DateTimeOffset SynchronizedAtUtc =
            DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

        [Fact]
        public async Task AddRangeAndGetByIds_PersistsOnlyRequestedFacilities()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new CareFacilityRepository(dbContext);
            CareFacility first = CreateFacility(Guid.NewGuid(), "Central Hospital");
            CareFacility second = CreateFacility(Guid.NewGuid(), "Regional Clinic");
            CareFacility unrequested = CreateFacility(Guid.NewGuid(), "South Clinic");

            await repository.AddRangeAsync([first, second, unrequested]);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<CareFacility> loaded = await repository.GetByIdsAsync(
                [first.CareFacilityId, second.CareFacilityId]);

            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, facility => facility.CareFacilityId == first.CareFacilityId);
            Assert.Contains(loaded, facility => facility.CareFacilityId == second.CareFacilityId);
            Assert.DoesNotContain(loaded, facility => facility.CareFacilityId == unrequested.CareFacilityId);
            Assert.All(loaded, facility => Assert.Equal(
                EntityState.Unchanged,
                dbContext.Entry(facility).State));
            Assert.All(loaded, facility => Assert.Equal(240, facility.DailyPatientCapacity));
        }

        [Fact]
        public async Task GetByIds_EmptyIds_ReturnsWithoutTrackingFacilities()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new CareFacilityRepository(dbContext);
            dbContext.CareFacilities.Add(CreateFacility(Guid.NewGuid(), "Central Hospital"));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<CareFacility> loaded = await repository.GetByIdsAsync(
                Array.Empty<CareFacilityId>());

            Assert.Empty(loaded);
            Assert.Empty(dbContext.ChangeTracker.Entries<CareFacility>());
        }

        [Fact]
        public void BatchLookup_TranslatesStrongIdsToPostgreSqlArrayPredicate()
        {
            DbContextOptions<HealthcareDbContext> options =
                new DbContextOptionsBuilder<HealthcareDbContext>()
                   .UseNpgsql("Host=localhost;Database=healthcare_translation_test;Username=test;Password=test")
                   .Options;
            using var dbContext = new HealthcareDbContext(options);
            CareFacilityId[] facilityIds =
            [
                new CareFacilityId(Guid.NewGuid()),
                new CareFacilityId(Guid.NewGuid())
            ];

            string sql = dbContext.CareFacilities
               .Where(facility => facilityIds.Contains(facility.Id))
               .ToQueryString();

            Assert.Contains("care_facility_id", sql, StringComparison.Ordinal);
            Assert.Contains("ANY", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static CareFacility CreateFacility(Guid facilityId, string name)
        {
            return CareFacility.Register(
                id: new CareFacilityId(facilityId),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                name: name,
                kind: new CareFacilityKindKey("Hospital"),
                locationAnchorId: new LocationAnchorId(Guid.NewGuid()),
                dailyPatientCapacity: 240,
                isActive: true,
                sourceRevision: 7,
                synchronizedAtUtc: SynchronizedAtUtc);
        }
    }
}

using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class EducationParticipationProjectionRepositoryTests
    {
        private static readonly Guid HostId =
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ResidentId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public async Task UpsertNewerAsync_InsertsUpdatesAndRejectsStaleRevision()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            await AddResidentAsync(dbContext);
            var repository = new EducationParticipationProjectionRepository(dbContext);

            int inserted = await repository.UpsertNewerAsync([CreateProjection(revision: 1)]);
            await dbContext.SaveChangesAsync();
            int updated = await repository.UpsertNewerAsync(
                [CreateProjection(revision: 2, isEnrolled: false)]);
            await dbContext.SaveChangesAsync();
            int stale = await repository.UpsertNewerAsync([CreateProjection(revision: 1)]);
            await dbContext.SaveChangesAsync();

            Assert.Equal(1, inserted);
            Assert.Equal(1, updated);
            Assert.Equal(0, stale);
            IReadOnlyDictionary<Guid, EducationParticipationProjection> stored =
                await repository.GetByResidentIdsAsync(HostId, [ResidentId]);
            EducationParticipationProjection projection = Assert.Single(stored).Value;
            Assert.Equal(2, projection.ParticipationRevision);
            Assert.False(projection.IsEnrolled);
        }

        [Fact]
        public async Task GetByResidentIdsAsync_ScopesProjectionToSimulationHost()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            await AddResidentAsync(dbContext);
            var repository = new EducationParticipationProjectionRepository(dbContext);
            await repository.UpsertNewerAsync([CreateProjection(revision: 1)]);
            await dbContext.SaveChangesAsync();

            IReadOnlyDictionary<Guid, EducationParticipationProjection> otherHost =
                await repository.GetByResidentIdsAsync(Guid.NewGuid(), [ResidentId]);

            Assert.Empty(otherHost);
        }

        [Fact]
        public async Task GetEnrolledByResidentIdsAsync_ReturnsOnlyActiveParticipation()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            await AddResidentAsync(dbContext);
            var repository = new EducationParticipationProjectionRepository(dbContext);
            await repository.UpsertNewerAsync([CreateProjection(revision: 1)]);
            await dbContext.SaveChangesAsync();

            IReadOnlyDictionary<Guid, EducationParticipationProjection> enrolled =
                await repository.GetEnrolledByResidentIdsAsync(HostId, [ResidentId]);
            await repository.UpsertNewerAsync([CreateProjection(revision: 2, isEnrolled: false)]);
            await dbContext.SaveChangesAsync();
            IReadOnlyDictionary<Guid, EducationParticipationProjection> withdrawn =
                await repository.GetEnrolledByResidentIdsAsync(HostId, [ResidentId]);

            Assert.True(Assert.Single(enrolled).Value.IsEnrolled);
            Assert.Empty(withdrawn);
        }

        [Fact]
        public async Task DeleteBySimulationHostAsync_RemovesOnlyRequestedHost()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            await AddResidentAsync(dbContext);
            var repository = new EducationParticipationProjectionRepository(dbContext);
            await repository.UpsertNewerAsync([CreateProjection(revision: 1)]);
            await dbContext.SaveChangesAsync();

            await repository.DeleteBySimulationHostAsync(HostId);
            await dbContext.SaveChangesAsync();

            Assert.Empty(dbContext.EducationParticipationProjections);
        }

        private static async Task AddResidentAsync(PopulationDbContext dbContext)
        {
            Person resident = CreatePerson(personId: ResidentId);
            dbContext.Households.Add(CreateHousehold(householdId: resident.HouseholdId.Value));
            dbContext.Persons.Add(resident);
            await dbContext.SaveChangesAsync();
        }

        private static EducationParticipationProjection CreateProjection(
            long revision,
            bool isEnrolled = true)
        {
            return new EducationParticipationProjection(
                SimulationHostId: HostId,
                ResidentId: ResidentId,
                ParticipationRevision: revision,
                ResidentLifecycleRevision: 0,
                IsEnrolled: isEnrolled,
                ActiveStage: isEnrolled ? "primary" : null,
                InstitutionId: isEnrolled
                    ? Guid.Parse("33333333-3333-3333-3333-333333333333")
                    : null,
                InstitutionAnchorId: isEnrolled
                    ? Guid.Parse("44444444-4444-4444-4444-444444444444")
                    : null,
                EnrolledOn: isEnrolled ? new DateOnly(2048, 5, 1) : null,
                CompletedStage: "preschool",
                CompletedStageOn: new DateOnly(2047, 6, 30),
                SnapshotDate: new DateOnly(2048, 5, 3),
                OccurredAtUtc: new DateTimeOffset(2048, 5, 3, 8, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc: new DateTimeOffset(2048, 5, 3, 8, 0, 1, TimeSpan.Zero));
        }
    }
}

using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Models;
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
        public async Task EconomicEffects_SurviveReloadAndCannotBeOverwrittenByStaleLegacyMessage()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            var dbContext = database.DbContext;
            await AddResidentAsync(dbContext);
            var repository = new EducationParticipationProjectionRepository(dbContext);
            var effects = new ResidentExternalEconomicProfile(ResidentAgeIncomeSchedule.Create((0, 3m), (21, 99m)),
                7m, 0.1d, 0.8d, -0.1m, 0.03m, 0.07m);
            await repository.UpsertNewerAsync([CreateProjection(2) with { Economics = effects }]);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            Assert.Equal(0, await repository.UpsertNewerAsync([CreateProjection(1)]));

            var restored = Assert.Single(await repository.GetByResidentIdsAsync(HostId, [ResidentId])).Value.Economics;
            Assert.NotNull(restored);
            Assert.Equal(99m, restored.TransferIncome.Resolve(21));
            Assert.Equal(0.8d, restored.EmploymentAvailabilityFactor);
            Assert.Equal(7m, restored.EmploymentIncomeBonus);

            await repository.UpsertNewerAsync([CreateProjection(3) with { Economics = ResidentExternalEconomicProfile.Neutral }]);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            restored = Assert.Single(await repository.GetByResidentIdsAsync(HostId, [ResidentId])).Value.Economics;
            Assert.NotNull(restored);
            Assert.Equal(0m, restored.TransferIncome.Resolve(21));
            Assert.Equal(1d, restored.EmploymentAvailabilityFactor);
        }

        [Fact]
        public async Task ReadBatch_ReusesEqualEconomicProfilesAcrossResidents()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            var dbContext = database.DbContext;
            await AddResidentAsync(dbContext);
            Person second = CreatePerson(personId: Guid.NewGuid());
            dbContext.Households.Add(CreateHousehold(householdId: second.HouseholdId.Value));
            dbContext.Persons.Add(second);
            await dbContext.SaveChangesAsync();
            var repository = new EducationParticipationProjectionRepository(dbContext);
            var first = CreateProjection(1) with { Economics = ResidentExternalEconomicProfile.Neutral };
            await repository.UpsertNewerAsync([first, first with { ResidentId = second.Id.Value }]);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var profiles = await repository.GetByResidentIdsAsync(HostId, [ResidentId, second.Id.Value]);
            Assert.NotNull(profiles[ResidentId].Economics);
            Assert.Same(profiles[ResidentId].Economics, profiles[second.Id.Value].Economics);
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

        [Fact]
        public async Task Attendance_RoundTripsAndRejectsStaleOrInvalidatedResults()
        {
            await using var database = CreateDbContext();
            var db = database.DbContext;
            await AddResidentAsync(db);
            var repository = new EducationParticipationProjectionRepository(db);
            var writer = new EducationAttendanceProjectionWriter(db);
            await repository.UpsertNewerAsync([CreateProjection(1)]);
            await db.SaveChangesAsync();
            var now = new DateTimeOffset(2048, 5, 3, 9, 0, 0, TimeSpan.Zero);
            EducationAttendanceInput input = new(ResidentId, 0, 1, 0.7m, 0.8m);
            Assert.Equal(1, await writer.ApplyAsync(HostId, 5, now, [input], default));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var attendance = Assert.Single(await repository.GetByResidentIdsAsync(HostId, [ResidentId])).Value.Attendance;
            Assert.NotNull(attendance);
            Assert.Equal(new EducationAttendanceProjection(5, now, 0.7m, 0.8m), attendance);
            Assert.Equal(0, await writer.ApplyAsync(HostId, 5, now, [input with { AttendanceIndex = 0.1m }], default));
            Assert.Equal(0, await writer.ApplyAsync(HostId, 4, now, [input], default));
            Assert.Equal(0, await writer.ApplyAsync(HostId, 6, now.AddHours(-1), [input], default));
            Assert.Equal(0, await writer.ApplyAsync(Guid.NewGuid(), 6, now, [input], default));
            Assert.Equal(0, await writer.ApplyAsync(HostId, 6, now, [input with { ResidentLifecycleRevision = 1 }], default));
            await repository.UpsertNewerAsync([CreateProjection(2)]);
            await db.SaveChangesAsync();
            Assert.Null(Assert.Single(await repository.GetByResidentIdsAsync(HostId, [ResidentId])).Value.Attendance);
            Assert.Equal(0, await writer.ApplyAsync(HostId, 6, now, [input], default));
            Assert.Equal(1, await writer.ApplyAsync(HostId, 6, now, [input with { ParticipationRevision = 2 }], default));
            await repository.UpsertNewerAsync([CreateProjection(3, false)]);
            Assert.Equal(0, await writer.ApplyAsync(HostId, 7, now, [input with { ParticipationRevision = 3 }], default));
            await repository.DeleteBySimulationHostAsync(HostId);
            await db.SaveChangesAsync();
            Assert.Equal(0, await writer.ApplyAsync(HostId, 8, now, [input], default));
            Assert.Empty(db.EducationParticipationProjections);
        }

        [Fact]
        public async Task Routines_SurviveReloadShareEqualValuesAndIgnoreStaleUpdates()
        {
            await using var database = CreateDbContext();
            var db = database.DbContext;
            await AddResidentAsync(db);
            var second = CreatePerson(personId: Guid.NewGuid());
            db.Households.Add(CreateHousehold(householdId: second.HouseholdId.Value));
            db.Persons.Add(second);
            await db.SaveChangesAsync();
            var repository = new EducationParticipationProjectionRepository(db);
            var routine = PersonRoutineProfile.Structured(TimeSpan.FromHours(10), TimeSpan.FromHours(18),
                PersonStructuredActivityLoad.Demanding, PersonRoutineDays.Saturday);
            var projection = CreateProjection(2) with { Routine = routine };
            await repository.UpsertNewerAsync([projection, projection with { ResidentId = second.Id.Value }]);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(0, await repository.UpsertNewerAsync([CreateProjection(1)]));
            var restored = await repository.GetByResidentIdsAsync(HostId, [ResidentId, second.Id.Value]);
            Assert.Equal(routine, restored[ResidentId].Routine);
            Assert.Same(restored[ResidentId].Routine, restored[second.Id.Value].Routine);
            var active = await repository.GetEnrolledByResidentIdsAsync(HostId, [ResidentId, second.Id.Value]);
            Assert.Same(active[ResidentId].Routine, active[second.Id.Value].Routine);
            Assert.Empty(await repository.GetByResidentIdsAsync(Guid.NewGuid(), [ResidentId]));

            await repository.UpsertNewerAsync([CreateProjection(3) with { Routine = PersonRoutineProfile.Unstructured }]);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Same(PersonRoutineProfile.Unstructured, (await repository.GetByResidentIdsAsync(HostId, [ResidentId]))[ResidentId].Routine);
            await repository.UpsertNewerAsync([CreateProjection(4, false) with { Routine = PersonRoutineProfile.Unstructured }]);
            await db.SaveChangesAsync();
            Assert.Empty(await repository.GetEnrolledByResidentIdsAsync(HostId, [ResidentId]));
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

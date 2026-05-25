using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public sealed class SyncCityEnvironmentCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            var unitOfWork = new FakeUnitOfWork();
            SyncCityEnvironmentCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                environmentRepository: environmentRepository,
                unitOfWork: unitOfWork);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: CreateCommand(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.CityDeleted,
                actual: result.Status);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: deletionStateRepository.RequestedCityId);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: environmentRepository.RequestedCityId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(environmentRepository.UpsertedEnvironments);
        }

        [Fact]
        public async Task Handle_WhenCityIsArchived_ReturnsArchivedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            SyncCityEnvironmentCommandHandler handler = CreateHandler(archiveStateRepository: archiveStateRepository);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: CreateCommand(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.CityArchived,
                actual: result.Status);
        }

        [Fact]
        public async Task Handle_WhenEnvironmentDoesNotExist_UpsertsAndReturnsApplied()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateTimeOffset syncedAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 17,
                minute: 20,
                second: 0,
                offset: TimeSpan.Zero);
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            var unitOfWork = new FakeUnitOfWork();
            SyncCityEnvironmentCommandHandler handler = CreateHandler(
                environmentRepository: environmentRepository,
                unitOfWork: unitOfWork);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    syncedAtUtc: syncedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.Applied,
                actual: result.Status);
            CityPopulationEnvironment environment = Assert.Single(environmentRepository.UpsertedEnvironments);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: environment.CityId);
            Assert.Equal(
                expected: PopulationClimateZone.Temperate,
                actual: environment.ClimateZone);
            Assert.Equal(
                expected: PopulationHemisphere.Northern,
                actual: environment.Hemisphere);
            Assert.Equal(
                expected: 180,
                actual: environment.UtcOffsetMinutes);
            Assert.Equal(
                expected: syncedAtUtc,
                actual: environment.CreatedAtUtc);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenSyncIsStale_ReturnsStaleWithoutSaving()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var environmentRepository = new FakeCityPopulationEnvironmentRepository
            {
                State = CityPopulationEnvironment.Create(
                    cityId: CityId.From(cityId),
                    climateZone: PopulationClimateZone.Temperate,
                    hemisphere: PopulationHemisphere.Northern,
                    utcOffsetMinutes: 180,
                    createdAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            SyncCityEnvironmentCommandHandler handler = CreateHandler(
                environmentRepository: environmentRepository,
                unitOfWork: unitOfWork);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    climateZone: "Continental",
                    syncedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 29,
                        second: 0,
                        offset: TimeSpan.Zero)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: PopulationClimateZone.Temperate,
                actual: environmentRepository.State!.ClimateZone);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(environmentRepository.UpsertedEnvironments);
        }

        [Fact]
        public async Task Handle_WhenSyncIsDuplicate_ReturnsDuplicate()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateTimeOffset syncedAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 17,
                minute: 35,
                second: 0,
                offset: TimeSpan.Zero);
            var environmentRepository = new FakeCityPopulationEnvironmentRepository
            {
                State = CityPopulationEnvironment.Create(
                    cityId: CityId.From(cityId),
                    climateZone: PopulationClimateZone.Temperate,
                    hemisphere: PopulationHemisphere.Northern,
                    utcOffsetMinutes: 180,
                    createdAtUtc: syncedAtUtc)
            };
            var unitOfWork = new FakeUnitOfWork();
            SyncCityEnvironmentCommandHandler handler = CreateHandler(
                environmentRepository: environmentRepository,
                unitOfWork: unitOfWork);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    syncedAtUtc: syncedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenEnvironmentChanges_UpdatesExistingEnvironmentAndSaves()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var environmentRepository = new FakeCityPopulationEnvironmentRepository
            {
                State = CityPopulationEnvironment.Create(
                    cityId: CityId.From(cityId),
                    climateZone: PopulationClimateZone.Temperate,
                    hemisphere: PopulationHemisphere.Northern,
                    utcOffsetMinutes: 180,
                    createdAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 40,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            SyncCityEnvironmentCommandHandler handler = CreateHandler(
                environmentRepository: environmentRepository,
                unitOfWork: unitOfWork);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    climateZone: "Continental",
                    hemisphere: "Southern",
                    utcOffsetMinutes: -120,
                    syncedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 50,
                        second: 0,
                        offset: TimeSpan.Zero)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: PopulationClimateZone.Continental,
                actual: environmentRepository.State!.ClimateZone);
            Assert.Equal(
                expected: PopulationHemisphere.Southern,
                actual: environmentRepository.State.Hemisphere);
            Assert.Equal(
                expected: -120,
                actual: environmentRepository.State.UtcOffsetMinutes);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_ApplyCityEnvironmentSyncCommand_UsesSameFlow()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            SyncCityEnvironmentCommandHandler handler = CreateHandler(environmentRepository: environmentRepository);

            SyncCityEnvironmentResult result = await handler.Handle(
                request: new ApplyCityEnvironmentSyncCommand(
                    CityId: cityId,
                    ClimateZone: "Mountain",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 60,
                    SyncedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 55,
                        second: 0,
                        offset: TimeSpan.Zero)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityEnvironmentStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: PopulationClimateZone.Mountain,
                actual: Assert.Single(environmentRepository.UpsertedEnvironments)
                   .ClimateZone);
        }

        private static SyncCityEnvironmentCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationEnvironmentRepository? environmentRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new SyncCityEnvironmentCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                cityPopulationEnvironmentRepository: environmentRepository ??
                                                     new FakeCityPopulationEnvironmentRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static SyncCityEnvironmentCommand CreateCommand(
            Guid cityId,
            string climateZone = "Temperate",
            string hemisphere = "Northern",
            int utcOffsetMinutes = 180,
            DateTimeOffset? syncedAtUtc = null)
        {
            return new SyncCityEnvironmentCommand(
                CityId: cityId,
                ClimateZone: climateZone,
                Hemisphere: hemisphere,
                UtcOffsetMinutes: utcOffsetMinutes,
                SyncedAtUtc: syncedAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 17,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}

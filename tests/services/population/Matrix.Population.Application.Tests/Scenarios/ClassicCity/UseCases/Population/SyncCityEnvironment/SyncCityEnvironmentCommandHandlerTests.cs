using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;

public sealed class SyncCityEnvironmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var environmentRepository = new FakeCityPopulationEnvironmentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            environmentRepository: environmentRepository,
            unitOfWork: unitOfWork);

        SyncCityEnvironmentResult result = await handler.Handle(
            CreateCommand(cityId),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.CityDeleted, result.Status);
        Assert.Equal(CityId.From(cityId), deletionStateRepository.RequestedCityId);
        Assert.Equal(CityId.From(cityId), environmentRepository.RequestedCityId);
        Assert.Equal(1, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(environmentRepository.UpsertedEnvironments);
    }

    [Fact]
    public async Task Handle_WhenCityIsArchived_ReturnsArchivedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
        {
            State = CityPopulationArchiveState.Create(
                cityId: CityId.From(cityId),
                archivedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var handler = CreateHandler(archiveStateRepository: archiveStateRepository);

        SyncCityEnvironmentResult result = await handler.Handle(
            CreateCommand(cityId),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.CityArchived, result.Status);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentDoesNotExist_UpsertsAndReturnsApplied()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DateTimeOffset syncedAtUtc = new(2048, 5, 3, 17, 20, 0, TimeSpan.Zero);
        var environmentRepository = new FakeCityPopulationEnvironmentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            environmentRepository: environmentRepository,
            unitOfWork: unitOfWork);

        SyncCityEnvironmentResult result = await handler.Handle(
            CreateCommand(cityId, syncedAtUtc: syncedAtUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.Applied, result.Status);
        CityPopulationEnvironment environment = Assert.Single(environmentRepository.UpsertedEnvironments);
        Assert.Equal(CityId.From(cityId), environment.CityId);
        Assert.Equal(PopulationClimateZone.Temperate, environment.ClimateZone);
        Assert.Equal(PopulationHemisphere.Northern, environment.Hemisphere);
        Assert.Equal(180, environment.UtcOffsetMinutes);
        Assert.Equal(syncedAtUtc, environment.CreatedAtUtc);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenSyncIsStale_ReturnsStaleWithoutSaving()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var environmentRepository = new FakeCityPopulationEnvironmentRepository
        {
            State = CityPopulationEnvironment.Create(
                cityId: CityId.From(cityId),
                climateZone: PopulationClimateZone.Temperate,
                hemisphere: PopulationHemisphere.Northern,
                utcOffsetMinutes: 180,
                createdAtUtc: new DateTimeOffset(2048, 5, 3, 17, 30, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            environmentRepository: environmentRepository,
            unitOfWork: unitOfWork);

        SyncCityEnvironmentResult result = await handler.Handle(
            CreateCommand(
                cityId,
                climateZone: "Continental",
                syncedAtUtc: new DateTimeOffset(2048, 5, 3, 17, 29, 0, TimeSpan.Zero)),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.Stale, result.Status);
        Assert.Equal(PopulationClimateZone.Temperate, environmentRepository.State!.ClimateZone);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(environmentRepository.UpsertedEnvironments);
    }

    [Fact]
    public async Task Handle_WhenSyncIsDuplicate_ReturnsDuplicate()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DateTimeOffset syncedAtUtc = new(2048, 5, 3, 17, 35, 0, TimeSpan.Zero);
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
        var handler = CreateHandler(
            environmentRepository: environmentRepository,
            unitOfWork: unitOfWork);

        SyncCityEnvironmentResult result = await handler.Handle(
            CreateCommand(cityId, syncedAtUtc: syncedAtUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.Duplicate, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentChanges_UpdatesExistingEnvironmentAndSaves()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var environmentRepository = new FakeCityPopulationEnvironmentRepository
        {
            State = CityPopulationEnvironment.Create(
                cityId: CityId.From(cityId),
                climateZone: PopulationClimateZone.Temperate,
                hemisphere: PopulationHemisphere.Northern,
                utcOffsetMinutes: 180,
                createdAtUtc: new DateTimeOffset(2048, 5, 3, 17, 40, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            environmentRepository: environmentRepository,
            unitOfWork: unitOfWork);

        SyncCityEnvironmentResult result = await handler.Handle(
            CreateCommand(
                cityId,
                climateZone: "Continental",
                hemisphere: "Southern",
                utcOffsetMinutes: -120,
                syncedAtUtc: new DateTimeOffset(2048, 5, 3, 17, 50, 0, TimeSpan.Zero)),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.Applied, result.Status);
        Assert.Equal(PopulationClimateZone.Continental, environmentRepository.State!.ClimateZone);
        Assert.Equal(PopulationHemisphere.Southern, environmentRepository.State.Hemisphere);
        Assert.Equal(-120, environmentRepository.State.UtcOffsetMinutes);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_ApplyCityEnvironmentSyncCommand_UsesSameFlow()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var environmentRepository = new FakeCityPopulationEnvironmentRepository();
        var handler = CreateHandler(environmentRepository: environmentRepository);

        SyncCityEnvironmentResult result = await handler.Handle(
            new ApplyCityEnvironmentSyncCommand(
                CityId: cityId,
                ClimateZone: "Mountain",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 60,
                SyncedAtUtc: new DateTimeOffset(2048, 5, 3, 17, 55, 0, TimeSpan.Zero)),
            CancellationToken.None);

        Assert.Equal(SyncCityEnvironmentStatus.Applied, result.Status);
        Assert.Equal(PopulationClimateZone.Mountain, Assert.Single(environmentRepository.UpsertedEnvironments).ClimateZone);
    }

    private static SyncCityEnvironmentCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationEnvironmentRepository? environmentRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new SyncCityEnvironmentCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            environmentRepository ?? new FakeCityPopulationEnvironmentRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
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
            SyncedAtUtc: syncedAtUtc ?? new DateTimeOffset(2048, 5, 3, 17, 15, 0, TimeSpan.Zero));
    }
}

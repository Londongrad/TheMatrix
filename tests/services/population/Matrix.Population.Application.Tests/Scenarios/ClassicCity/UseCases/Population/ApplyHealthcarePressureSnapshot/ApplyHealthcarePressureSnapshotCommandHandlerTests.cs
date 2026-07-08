using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population
    .ApplyHealthcarePressureSnapshot;

public sealed class ApplyHealthcarePressureSnapshotCommandHandlerTests
{
    private static readonly Guid CityGuid =
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task Handle_WhenSnapshotIsNewer_StoresHealthcareProjection()
    {
        var repository = new HealthcarePressureSnapshotRepositoryStub();
        var unitOfWork = new FakeUnitOfWork();
        ApplyHealthcarePressureSnapshotCommandHandler handler = CreateHandler(
            repository,
            unitOfWork: unitOfWork);

        ApplyHealthcarePressureSnapshotResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(ApplyHealthcarePressureSnapshotStatus.Applied, result.Status);
        Assert.NotNull(repository.Snapshot);
        Assert.Equal(17, repository.Snapshot.SourceRevision);
        Assert.Equal(100, repository.Snapshot.PatientCount);
        Assert.Equal(8, repository.Snapshot.Pressure.ActiveIllnessCount);
        Assert.Equal(2, repository.Snapshot.Pressure.SevereIllnessCount);
        Assert.Equal(0.82m, repository.Snapshot.Pressure.MedicalLoadIndex);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenRevisionIsNotNewer_ReturnsStale()
    {
        var repository = new HealthcarePressureSnapshotRepositoryStub
        {
            Snapshot = CreateSnapshot(sourceRevision: 18)
        };
        var unitOfWork = new FakeUnitOfWork();
        ApplyHealthcarePressureSnapshotCommandHandler handler = CreateHandler(
            repository,
            unitOfWork: unitOfWork);

        ApplyHealthcarePressureSnapshotResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(ApplyHealthcarePressureSnapshotStatus.Stale, result.Status);
        Assert.Equal(18, repository.Snapshot.SourceRevision);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenMessageWasProcessed_ReturnsDuplicate()
    {
        var processedRepository = new FakeProcessedIntegrationMessageRepository
        {
            TryMarkProcessedResult = false
        };
        var repository = new HealthcarePressureSnapshotRepositoryStub();
        ApplyHealthcarePressureSnapshotCommandHandler handler = CreateHandler(
            repository,
            processedRepository: processedRepository);

        ApplyHealthcarePressureSnapshotResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(ApplyHealthcarePressureSnapshotStatus.Duplicate, result.Status);
        Assert.Null(repository.Snapshot);
    }

    [Fact]
    public async Task Handle_WhenCountsAreInconsistent_RejectsBeforeTransaction()
    {
        var unitOfWork = new FakeUnitOfWork();
        ApplyHealthcarePressureSnapshotCommandHandler handler = CreateHandler(
            new HealthcarePressureSnapshotRepositoryStub(),
            unitOfWork: unitOfWork);
        ApplyHealthcarePressureSnapshotCommand invalid = CreateCommand() with
        {
            PatientCount = 2,
            ActiveIllnessCount = 3
        };

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            invalid,
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
    }

    private static ApplyHealthcarePressureSnapshotCommandHandler CreateHandler(
        HealthcarePressureSnapshotRepositoryStub repository,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyHealthcarePressureSnapshotCommandHandler(
            archiveStateRepository: new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository: new FakeCityPopulationDeletionStateRepository(),
            snapshotRepository: repository,
            processedMessageRepository: processedRepository ??
                                        new FakeProcessedIntegrationMessageRepository(),
            timeProvider: CreateTimeProvider(),
            unitOfWork: unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyHealthcarePressureSnapshotCommand CreateCommand()
    {
        return new ApplyHealthcarePressureSnapshotCommand(
            CityId: CityGuid,
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-health-pressure",
            SourceRevision: 17,
            CurrentDate: new DateOnly(2048, 5, 6),
            PatientCount: 100,
            ActiveIllnessCount: 8,
            SevereIllnessCount: 2,
            MedicalLoadIndex: 0.82m,
            TriagePressureIndex: 0.34m,
            RecoverySupportIndex: 1.12m,
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero));
    }

    private static ClassicCityHealthcarePressureSnapshot CreateSnapshot(long sourceRevision)
    {
        return new ClassicCityHealthcarePressureSnapshot(
            CityId: CityId.From(CityGuid),
            SourceRevision: sourceRevision,
            CurrentDate: new DateOnly(2048, 5, 6),
            PatientCount: 100,
            Pressure: new CityPopulationHealthcarePressureProfile(8, 2, 0.82m, 0.34m, 1.12m),
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 1, 0, TimeSpan.Zero));
    }

    private sealed class HealthcarePressureSnapshotRepositoryStub
        : ICityHealthcarePressureSnapshotRepository
    {
        internal ClassicCityHealthcarePressureSnapshot? Snapshot { get; set; }

        public Task<ClassicCityHealthcarePressureSnapshot?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Snapshot?.CityId == cityId ? Snapshot : null);
        }

        public Task UpsertAsync(
            ClassicCityHealthcarePressureSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            if (Snapshot?.CityId == cityId)
                Snapshot = null;
            return Task.CompletedTask;
        }
    }
}

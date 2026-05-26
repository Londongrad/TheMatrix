using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles
{
    public sealed class SeedCityStockpilesTests
    {
        [Fact]
        public void Validator_RejectsEmptyIdsAndNonUtcTimestamp()
        {
            var validator = new SeedCityStockpilesCommandValidator();

            ValidationResult? result = validator.Validate(
                new SeedCityStockpilesCommand(
                    CityId: Guid.Empty,
                    CreatedAtUtc: new DateTimeOffset(
                        year: 2049,
                        month: 1,
                        day: 1,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(9)),
                    SimulationKind: string.Empty,
                    DevelopmentLevel: "advanced"));

            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 3);
        }

        [Fact]
        public async Task Handler_IgnoresNonClassicCitySimulationKind()
        {
            var repository = new FakeCityStockpileRepository();
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
            var handler = new SeedCityStockpilesCommandHandler(
                repository: repository,
                deletionStateRepository: new FakeCityResourceDeletionStateRepository(),
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            SeedCityStockpilesResult result = await handler.Handle(
                request: new SeedCityStockpilesCommand(
                    CityId: CityId,
                    CreatedAtUtc: CreatedAtUtc,
                    SimulationKind: "Sandbox",
                    DevelopmentLevel: "advanced"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityStockpilesStatus.IgnoredSimulationKind,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: repository.AddCallCount);
            Assert.Empty(outboxWriter.Snapshots);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handler_ReturnsDuplicateForExistingState()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState(emergencyRationingEnabled: true)
            };
            var handler = new SeedCityStockpilesCommandHandler(
                repository: repository,
                deletionStateRepository: new FakeCityResourceDeletionStateRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityStockpileSnapshotOutboxWriter(),
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            SeedCityStockpilesResult result = await handler.Handle(
                request: new SeedCityStockpilesCommand(
                    CityId: CityId,
                    CreatedAtUtc: CreatedAtUtc,
                    SimulationKind: "ClassicCity",
                    DevelopmentLevel: "advanced"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityStockpilesStatus.Duplicate,
                actual: result.Status);
            Assert.True(result.EmergencyRationingEnabled);
            Assert.Equal(
                expected: repository.State.SupplyStressIndex,
                actual: result.SupplyStressIndex);
        }

        [Fact]
        public async Task Handler_ReturnsCityDeletedWhenDeletionTombstoneExists()
        {
            var repository = new FakeCityStockpileRepository();
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
            var handler = new SeedCityStockpilesCommandHandler(
                repository: repository,
                deletionStateRepository: new FakeCityResourceDeletionStateRepository
                {
                    DeletedAtUtc = LaterUtc
                },
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            SeedCityStockpilesResult result = await handler.Handle(
                request: new SeedCityStockpilesCommand(
                    CityId: CityId,
                    CreatedAtUtc: CreatedAtUtc,
                    SimulationKind: "ClassicCity",
                    DevelopmentLevel: "advanced"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityStockpilesStatus.CityDeleted,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: repository.AddCallCount);
            Assert.Empty(outboxWriter.Snapshots);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handler_CreatesSeedStateAndWritesSnapshotWithInjectedTime()
        {
            var repository = new FakeCityStockpileRepository();
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
            DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(15);
            var handler = new SeedCityStockpilesCommandHandler(
                repository: repository,
                deletionStateRepository: new FakeCityResourceDeletionStateRepository(),
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider(occurredAtUtc));

            SeedCityStockpilesResult result = await handler.Handle(
                request: new SeedCityStockpilesCommand(
                    CityId: CityId,
                    CreatedAtUtc: CreatedAtUtc,
                    SimulationKind: "ClassicCity",
                    DevelopmentLevel: "struggling"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityStockpilesStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: repository.AddCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Single(outboxWriter.Snapshots);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: outboxWriter.Snapshots[0].OccurredAtUtc);
            Assert.Equal(
                expected: CityId,
                actual: outboxWriter.Snapshots[0].CityId);
        }
    }
}

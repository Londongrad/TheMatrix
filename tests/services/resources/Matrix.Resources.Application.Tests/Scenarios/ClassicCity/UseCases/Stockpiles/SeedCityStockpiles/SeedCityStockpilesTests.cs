using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using Matrix.Resources.Application.Tests.TestSupport;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles;

public sealed class SeedCityStockpilesTests
{
    [Fact]
    public void Validator_RejectsEmptyIdsAndNonUtcTimestamp()
    {
        var validator = new SeedCityStockpilesCommandValidator();

        var result = validator.Validate(new SeedCityStockpilesCommand(
            CityId: Guid.Empty,
            CreatedAtUtc: new DateTimeOffset(2049, 1, 1, 18, 0, 0, TimeSpan.FromHours(9)),
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
            repository,
            unitOfWork,
            outboxWriter,
            new CityStockpilePolicy(),
            CreateTimeProvider());

        SeedCityStockpilesResult result = await handler.Handle(
            new SeedCityStockpilesCommand(CityId, CreatedAtUtc, "Sandbox", "advanced"),
            CancellationToken.None);

        Assert.Equal(SeedCityStockpilesStatus.IgnoredSimulationKind, result.Status);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Empty(outboxWriter.Snapshots);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handler_ReturnsDuplicateForExistingState()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState(emergencyRationingEnabled: true)
        };
        var handler = new SeedCityStockpilesCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityStockpilePolicy(),
            CreateTimeProvider());

        SeedCityStockpilesResult result = await handler.Handle(
            new SeedCityStockpilesCommand(CityId, CreatedAtUtc, "ClassicCity", "advanced"),
            CancellationToken.None);

        Assert.Equal(SeedCityStockpilesStatus.Duplicate, result.Status);
        Assert.True(result.EmergencyRationingEnabled);
        Assert.Equal(repository.State.SupplyStressIndex, result.SupplyStressIndex);
    }

    [Fact]
    public async Task Handler_CreatesSeedStateAndWritesSnapshotWithInjectedTime()
    {
        var repository = new FakeCityStockpileRepository();
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
        DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(15);
        var handler = new SeedCityStockpilesCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new CityStockpilePolicy(),
            CreateTimeProvider(occurredAtUtc));

        SeedCityStockpilesResult result = await handler.Handle(
            new SeedCityStockpilesCommand(CityId, CreatedAtUtc, "ClassicCity", "struggling"),
            CancellationToken.None);

        Assert.Equal(SeedCityStockpilesStatus.Applied, result.Status);
        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(outboxWriter.Snapshots);
        Assert.Equal(occurredAtUtc, outboxWriter.Snapshots[0].OccurredAtUtc);
        Assert.Equal(CityId, outboxWriter.Snapshots[0].CityId);
    }
}

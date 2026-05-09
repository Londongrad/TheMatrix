using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;

public sealed class AdvanceCityStockpilesHandlerTests
{
    [Fact]
    public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
    {
        var handler = new AdvanceCityStockpilesCommandHandler(
            new FakeCityStockpileRepository(),
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityStockpilePolicy(),
            CreateTimeProvider());

        AdvanceCityStockpilesResult result = await handler.Handle(
            new AdvanceCityStockpilesCommand(CityId, CreatedAtUtc, LaterUtc, 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityStockpilesStatus.NotInitialized, result.Status);
    }

    [Fact]
    public async Task Handler_ReturnsOutOfOrderWhenTickMovesBackward()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.MarkTickApplied(5);
        var handler = new AdvanceCityStockpilesCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityStockpilePolicy(),
            CreateTimeProvider());

        AdvanceCityStockpilesResult result = await handler.Handle(
            new AdvanceCityStockpilesCommand(CityId, CreatedAtUtc, LaterUtc, 4),
            CancellationToken.None);

        Assert.Equal(AdvanceCityStockpilesStatus.OutOfOrder, result.Status);
    }

    [Fact]
    public async Task Handler_ReturnsDuplicateWhenTickWasAlreadyApplied()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.MarkTickApplied(5);
        var handler = new AdvanceCityStockpilesCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityStockpilePolicy(),
            CreateTimeProvider());

        AdvanceCityStockpilesResult result = await handler.Handle(
            new AdvanceCityStockpilesCommand(CityId, CreatedAtUtc, LaterUtc, 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityStockpilesStatus.Duplicate, result.Status);
    }

    [Fact]
    public async Task Handler_AdvancesStateAppliesDueResupplyAndWritesSnapshot()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.MarkTickApplied(4);
        repository.State.ScheduleResupply(
            focus: ResupplyFocus.Fuel,
            intensity: ResupplyIntensity.Medium,
            focusDistrictId: null,
            readyAtTickId: 5);
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
        DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(20);
        var handler = new AdvanceCityStockpilesCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new CityStockpilePolicy(),
            CreateTimeProvider(occurredAtUtc));

        AdvanceCityStockpilesResult result = await handler.Handle(
            new AdvanceCityStockpilesCommand(CityId, CreatedAtUtc, LaterUtc, 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityStockpilesStatus.Applied, result.Status);
        Assert.True(result.ProcessedSimMinutes > 0);
        Assert.Equal(5, repository.State!.LastAppliedTickId);
        Assert.False(repository.State.PendingResupply.IsScheduled);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(outboxWriter.Snapshots);
        Assert.Equal(occurredAtUtc, outboxWriter.Snapshots[0].OccurredAtUtc);
    }
}

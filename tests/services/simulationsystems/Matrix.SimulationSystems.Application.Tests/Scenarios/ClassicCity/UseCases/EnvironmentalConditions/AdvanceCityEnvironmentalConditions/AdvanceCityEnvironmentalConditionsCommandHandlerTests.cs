using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions;

public sealed class AdvanceCityEnvironmentalConditionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = CreateHandler(repository);

        AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(tickId: 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEnvironmentalConditionsStatus.NotInitialized, result.Status);
        Assert.Equal(0m, result.ProcessedSimMinutes);
    }

    [Fact]
    public async Task Handle_WhenRequestIsOutOfOrder_ReturnsOutOfOrder()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.MarkTickApplied(6);
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var handler = CreateHandler(repository);

        AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(
                fromUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                toUtc: SimulationSystemsApplicationTestSupport.LaterUtc,
                tickId: 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEnvironmentalConditionsStatus.OutOfOrder, result.Status);
        Assert.Equal(0m, result.ProcessedSimMinutes);
    }

    [Fact]
    public async Task Handle_WhenTickWasAlreadyApplied_ReturnsDuplicate()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.MarkTickApplied(5);
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var handler = CreateHandler(repository);

        AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(tickId: 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEnvironmentalConditionsStatus.Duplicate, result.Status);
        Assert.Equal(0m, result.ProcessedSimMinutes);
    }

    [Fact]
    public async Task Handle_WhenAdvanceSucceeds_UpdatesStateAndWritesOutboxUsingTimeProvider()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var systemsOutbox = new FakeCitySystemsResourceDemandOutboxWriter();
        var populationOutbox = new FakeCityPopulationLivingConditionsOutboxWriter();
        DateTimeOffset occurredAtUtc = SimulationSystemsApplicationTestSupport.CreatedAtUtc.AddHours(6);
        var handler = CreateHandler(
            repository,
            unitOfWork,
            systemsOutbox,
            populationOutbox,
            new FrozenTimeProvider(occurredAtUtc));

        AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(
                fromUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                toUtc: SimulationSystemsApplicationTestSupport.LaterUtc,
                tickId: 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEnvironmentalConditionsStatus.Applied, result.Status);
        Assert.Equal(120m, result.ProcessedSimMinutes);
        Assert.Equal(5, state.LastAppliedTickId);
        Assert.Equal(SimulationSystemsApplicationTestSupport.LaterUtc, state.LastEvaluatedAtUtc);
        Assert.Single(systemsOutbox.Snapshots);
        Assert.Single(populationOutbox.Snapshots);
        Assert.Equal(occurredAtUtc, systemsOutbox.Snapshots[0].OccurredAtUtc);
        Assert.Equal(occurredAtUtc, populationOutbox.Snapshots[0].OccurredAtUtc);
        Assert.Equal(5, systemsOutbox.Snapshots[0].EffectiveTickId);
        Assert.Equal(5, populationOutbox.Snapshots[0].EffectiveTickId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenSaveHitsConcurrency_ReturnsDuplicate()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork
        {
            SaveException = new DbUpdateConcurrencyException("race")
        };
        var systemsOutbox = new FakeCitySystemsResourceDemandOutboxWriter();
        var populationOutbox = new FakeCityPopulationLivingConditionsOutboxWriter();
        var handler = CreateHandler(repository, unitOfWork, systemsOutbox, populationOutbox);

        AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(tickId: 5),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEnvironmentalConditionsStatus.Duplicate, result.Status);
        Assert.Single(systemsOutbox.Snapshots);
        Assert.Single(populationOutbox.Snapshots);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static AdvanceCityEnvironmentalConditionsCommandHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        FakeCitySystemsResourceDemandOutboxWriter? systemsOutbox = null,
        FakeCityPopulationLivingConditionsOutboxWriter? populationOutbox = null,
        TimeProvider? timeProvider = null)
    {
        return new AdvanceCityEnvironmentalConditionsCommandHandler(
            repository,
            unitOfWork ?? new FakeUnitOfWork(),
            systemsOutbox ?? new FakeCitySystemsResourceDemandOutboxWriter(),
            populationOutbox ?? new FakeCityPopulationLivingConditionsOutboxWriter(),
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory(),
            timeProvider ?? SimulationSystemsApplicationTestSupport.CreateTimeProvider());
    }

    private static AdvanceCityEnvironmentalConditionsCommand CreateCommand(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        long tickId = 5)
    {
        return new AdvanceCityEnvironmentalConditionsCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            FromSimTimeUtc: fromUtc ?? SimulationSystemsApplicationTestSupport.CreatedAtUtc,
            ToSimTimeUtc: toUtc ?? SimulationSystemsApplicationTestSupport.LaterUtc,
            TickId: tickId);
    }
}

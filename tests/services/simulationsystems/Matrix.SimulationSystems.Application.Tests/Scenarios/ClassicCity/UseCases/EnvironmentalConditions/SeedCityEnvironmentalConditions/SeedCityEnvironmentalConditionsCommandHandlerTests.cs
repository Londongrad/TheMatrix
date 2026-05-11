using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions;

public sealed class SeedCityEnvironmentalConditionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSimulationKindDoesNotMatch_ReturnsIgnored()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var outbox = new FakeCityPopulationLivingConditionsOutboxWriter();
        var handler = CreateHandler(repository, unitOfWork, outbox);

        SeedCityEnvironmentalConditionsResult result = await handler.Handle(
            new SeedCityEnvironmentalConditionsCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                SimulationKind: "Sandbox",
                DevelopmentLevel: "standard"),
            CancellationToken.None);

        Assert.Equal(SeedCityEnvironmentalConditionsStatus.IgnoredSimulationKind, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(outbox.Snapshots);
    }

    [Fact]
    public async Task Handle_WhenStateAlreadyExists_ReturnsDuplicate()
    {
        var repository = new FakeCityEnvironmentalConditionRepository
        {
            State = SimulationSystemsApplicationTestSupport.CreateState()
        };
        var handler = CreateHandler(repository, new FakeUnitOfWork(), new FakeCityPopulationLivingConditionsOutboxWriter());

        SeedCityEnvironmentalConditionsResult result = await handler.Handle(
            new SeedCityEnvironmentalConditionsCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                SimulationKind: "ClassicCity",
                DevelopmentLevel: "standard"),
            CancellationToken.None);

        Assert.Equal(SeedCityEnvironmentalConditionsStatus.Duplicate, result.Status);
        Assert.Equal(repository.State.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenSeedSucceeds_AddsStateAndWritesOutboxWithInjectedTime()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var outbox = new FakeCityPopulationLivingConditionsOutboxWriter();
        DateTimeOffset occurredAtUtc = SimulationSystemsApplicationTestSupport.CreatedAtUtc.AddHours(4);
        var handler = CreateHandler(
            repository,
            unitOfWork,
            outbox,
            new FrozenTimeProvider(occurredAtUtc));

        SeedCityEnvironmentalConditionsResult result = await handler.Handle(
            new SeedCityEnvironmentalConditionsCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                SimulationKind: "ClassicCity",
                DevelopmentLevel: "advanced"),
            CancellationToken.None);

        Assert.Equal(SeedCityEnvironmentalConditionsStatus.Applied, result.Status);
        Assert.NotNull(repository.State);
        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(outbox.Snapshots);
        Assert.Equal(occurredAtUtc, outbox.Snapshots[0].OccurredAtUtc);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, outbox.Snapshots[0].CityId);
        Assert.Equal(repository.State.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
    }

    private static SeedCityEnvironmentalConditionsCommandHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeUnitOfWork unitOfWork,
        FakeCityPopulationLivingConditionsOutboxWriter outbox,
        TimeProvider? timeProvider = null)
    {
        return new SeedCityEnvironmentalConditionsCommandHandler(
            repository,
            unitOfWork,
            outbox,
            new CityEnvironmentalConditionPolicy(),
            timeProvider ?? SimulationSystemsApplicationTestSupport.CreateTimeProvider());
    }
}

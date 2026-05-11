using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure;

public sealed class SyncCityOperationalBudgetPressureCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(repository, new FakeUnitOfWork());

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.NotInitialized, result.Status);
    }

    [Fact]
    public async Task Handle_WhenSnapshotIsStale_ReturnsStale()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyOperationalBudgetPressure(new Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models.CityOperationalBudgetPressureSnapshot(
            Balance: 10m,
            MunicipalOperationsExpenses: 20m,
            GeneralAvailableAmount: 30m,
            OperationsAvailableAmount: 40m,
            InfrastructureAvailableAmount: 50m,
            HealthcareAvailableAmount: 60m,
            GeneralAuthorizationLevel: "High",
            OperationsAuthorizationLevel: "High",
            InfrastructureAuthorizationLevel: "High",
            HealthcareAuthorizationLevel: "High",
            PressureIndex: 0.10m,
            EffectiveTickId: 7,
            EffectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc));
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(repository, new FakeUnitOfWork());

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 6, effectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.Stale, result.Status);
        Assert.Equal(0.10m, result.PressureIndex);
    }

    [Fact]
    public async Task Handle_WhenSnapshotApplies_ReturnsApplied()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(repository, unitOfWork);

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 5, effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.Applied, result.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(0.72m, state.OperationalBudgetPressure.PressureIndex);
        Assert.Equal(5, state.OperationalBudgetPressure.EffectiveTickId);
    }

    [Fact]
    public async Task Handle_WhenConcurrencyPersistsMatchingSnapshot_ReturnsConcurrent()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork
        {
            SaveException = new DbUpdateConcurrencyException("race")
        };
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(repository, unitOfWork);

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 5, effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.Concurrent, result.Status);
        Assert.Equal(0.72m, result.PressureIndex);
    }

    private static SyncCityOperationalBudgetPressureCommand CreateCommand(
        long effectiveTickId = 5,
        DateTimeOffset? effectiveAtUtc = null)
    {
        return new SyncCityOperationalBudgetPressureCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            Balance: -50_000m,
            MunicipalOperationsExpenses: 300_000m,
            GeneralAvailableAmount: 80_000m,
            OperationsAvailableAmount: 70_000m,
            InfrastructureAvailableAmount: 60_000m,
            HealthcareAvailableAmount: 50_000m,
            GeneralAuthorizationLevel: "Restricted",
            OperationsAuthorizationLevel: "Emergency",
            InfrastructureAuthorizationLevel: "Restricted",
            HealthcareAuthorizationLevel: "Constrained",
            PressureIndex: 0.72m,
            EffectiveTickId: effectiveTickId,
            EffectiveAtUtc: effectiveAtUtc ?? SimulationSystemsApplicationTestSupport.CreatedAtUtc);
    }
}

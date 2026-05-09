using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;

public sealed class SyncCityOperationalBudgetPressureHandlerTests
{
    [Fact]
    public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
    {
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(
            new FakeCityStockpileRepository(),
            new FakeUnitOfWork());

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 8, effectiveAtUtc: LaterUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.NotInitialized, result.Status);
        Assert.Equal(0m, result.PressureIndex);
    }

    [Fact]
    public async Task Handler_ReturnsStaleWhenSnapshotMovesBackward()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.ApplyOperationalBudgetPressure(new CityOperationalBudgetPressureSnapshot(
            Balance: 250_000m,
            MunicipalOperationsExpenses: 30_000m,
            GeneralAvailableAmount: 150_000m,
            OperationsAvailableAmount: 130_000m,
            InfrastructureAvailableAmount: 120_000m,
            HealthcareAvailableAmount: 110_000m,
            GeneralAuthorizationLevel: "High",
            OperationsAuthorizationLevel: "Medium",
            InfrastructureAuthorizationLevel: "Medium",
            HealthcareAuthorizationLevel: "Low",
            PressureIndex: 0.44m,
            EffectiveTickId: 9,
            EffectiveAtUtc: LaterUtc));
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(
            repository,
            new FakeUnitOfWork());

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 9, effectiveAtUtc: CreatedAtUtc.AddMinutes(30), pressureIndex: 0.55m),
            CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.Stale, result.Status);
        Assert.Equal(0.44m, result.PressureIndex);
    }

    [Fact]
    public async Task Handler_AppliesFreshSnapshotAndPersistsState()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SyncCityOperationalBudgetPressureCommandHandler(
            repository,
            unitOfWork);

        SyncCityOperationalBudgetPressureResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 10, effectiveAtUtc: LaterUtc, pressureIndex: 0.63m),
            CancellationToken.None);

        Assert.Equal(SyncCityOperationalBudgetPressureStatus.Applied, result.Status);
        Assert.Equal(0.63m, repository.State!.OperationalBudgetPressure.PressureIndex);
        Assert.Equal(10, repository.State.OperationalBudgetPressure.EffectiveTickId);
        Assert.Equal(LaterUtc, repository.State.OperationalBudgetPressure.EffectiveAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static SyncCityOperationalBudgetPressureCommand CreateCommand(
        long effectiveTickId,
        DateTimeOffset effectiveAtUtc,
        decimal pressureIndex = 0.52m)
    {
        return new SyncCityOperationalBudgetPressureCommand(
            CityId: CityId,
            Balance: 200_000m,
            MunicipalOperationsExpenses: 25_000m,
            GeneralAvailableAmount: 140_000m,
            OperationsAvailableAmount: 120_000m,
            InfrastructureAvailableAmount: 115_000m,
            HealthcareAvailableAmount: 95_000m,
            GeneralAuthorizationLevel: "High",
            OperationsAuthorizationLevel: "Medium",
            InfrastructureAuthorizationLevel: "Medium",
            HealthcareAuthorizationLevel: "Low",
            PressureIndex: pressureIndex,
            EffectiveTickId: effectiveTickId,
            EffectiveAtUtc: effectiveAtUtc);
    }
}

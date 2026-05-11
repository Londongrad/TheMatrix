using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance;

public sealed class DispatchCityDrainageMaintenanceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var handler = CreateHandler(
            repository: new FakeCityEnvironmentalConditionRepository(),
            unitOfWork: new FakeUnitOfWork(),
            outboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
            client: new FakeCityBudgetAuthorizationClient(),
            timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenAuthorizationIsDenied_ReturnsBudgetDecisionWithoutMutations()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var client = new FakeCityBudgetAuthorizationClient
        {
            Decision = new CityBudgetAuthorizationDecision(
                Status: "Denied",
                RequestedIntensity: "Heavy",
                ApprovedIntensity: null,
                AuthorizationLevel: "Low",
                AvailableAmount: 95m,
                PressureIndex: 0.71m,
                EmergencyOverrideRequested: false,
                AuthorizedByEmergencyOverride: false,
                Summary: "Budget pressure blocks heavy maintenance.")
        };
        var handler = CreateHandler(
            repository,
            unitOfWork,
            outboxWriter,
            client,
            SimulationSystemsApplicationTestSupport.CreateTimeProvider());

        var result = await handler.Handle(
            CreateCommand(focus: "PumpRepairs", intensity: "Heavy"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Denied", result!.BudgetAuthorizationStatus);
        Assert.Equal("Low", result.BudgetAuthorizationLevel);
        Assert.Equal(95m, result.BudgetAvailableAmount);
        Assert.Null(result.AppliedIntensity);
        Assert.False(state.PendingDrainageMaintenance.IsScheduled);
        Assert.Empty(outboxWriter.Expenses);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, client.AuthorizeCallCount);
        Assert.NotNull(client.LastRequest);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, client.LastRequest!.CityId);
        Assert.Equal("Infrastructure", client.LastRequest.Category);
        Assert.Equal("DrainageMaintenanceDispatch", client.LastRequest.OperationKind);
        Assert.Equal("Heavy", client.LastRequest.RequestedIntensity);
        Assert.Equal(
            CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                systemName: "Drainage",
                focus: "PumpRepairs",
                intensity: "Heavy"),
            client.LastRequest.EstimatedAmount);
    }

    [Fact]
    public async Task Handle_WhenDispatchIsApplied_SchedulesWorkAndWritesExpense()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var client = new FakeCityBudgetAuthorizationClient();
        var timeProvider = SimulationSystemsApplicationTestSupport.CreateTimeProvider(
            SimulationSystemsApplicationTestSupport.LaterUtc.AddHours(4));
        var handler = CreateHandler(repository, unitOfWork, outboxWriter, client, timeProvider);

        var result = await handler.Handle(
            CreateCommand(focus: "NetworkStabilization", intensity: "Standard"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NotRequired", result!.BudgetAuthorizationStatus);
        Assert.Equal("Standard", result.RequestedIntensity);
        Assert.Equal("Standard", result.AppliedIntensity);
        Assert.Equal("Standard", result.BudgetAuthorizedIntensity);
        Assert.Equal(0, client.AuthorizeCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.True(state.PendingDrainageMaintenance.IsScheduled);
        Assert.Equal("NetworkStabilization", state.PendingDrainageMaintenance.Focus);
        Assert.Equal("Standard", state.PendingDrainageMaintenance.Intensity);
        Assert.Equal(1, state.PendingDrainageMaintenance.ReadyAtTickId);
        Assert.Single(outboxWriter.Expenses);
        Assert.Equal(timeProvider.GetUtcNow(), outboxWriter.Expenses[0].OccurredAtUtc);
        Assert.Equal(
            CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                systemName: "Drainage",
                focus: "NetworkStabilization",
                intensity: "Standard"),
            outboxWriter.Expenses[0].Amount);
    }

    private static DispatchCityDrainageMaintenanceCommandHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeUnitOfWork unitOfWork,
        FakeCityOperationalExpenseOutboxWriter outboxWriter,
        FakeCityBudgetAuthorizationClient client,
        FrozenTimeProvider timeProvider)
    {
        return new DispatchCityDrainageMaintenanceCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new ClassicCityWeatherPressureProfileFactory(),
            new CityMaintenanceBudgetGuard(),
            new CityMaintenanceBudgetAuthorizationService(client),
            timeProvider);
    }

    private static DispatchCityDrainageMaintenanceCommand CreateCommand(
        string focus = "Balanced",
        string intensity = "Standard",
        bool emergencyOverride = false)
    {
        return new DispatchCityDrainageMaintenanceCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            Focus: focus,
            Intensity: intensity,
            EmergencyOverride: emergencyOverride);
    }
}

using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse;

public sealed class DispatchCityUtilityIncidentResponseCommandHandlerTests
{
    private static readonly Guid FocusDistrictId = Guid.Parse("74000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var handler = CreateHandler(
            repository: new FakeCityEnvironmentalConditionRepository(),
            unitOfWork: new FakeUnitOfWork(),
            outboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
            client: new FakeCityBudgetAuthorizationClient(),
            tripDispatcher: new FakeCityOperationalTripDispatcher(),
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
                AvailableAmount: 135m,
                PressureIndex: 0.78m,
                EmergencyOverrideRequested: false,
                AuthorizedByEmergencyOverride: false,
                Summary: "Budget pressure blocks heavy utility incident response.")
        };
        var tripDispatcher = new FakeCityOperationalTripDispatcher();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            outboxWriter,
            client,
            tripDispatcher,
            SimulationSystemsApplicationTestSupport.CreateTimeProvider());

        var result = await handler.Handle(
            CreateCommand(focus: "PowerOutages", intensity: "Heavy", focusDistrictId: FocusDistrictId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Denied", result!.BudgetAuthorizationStatus);
        Assert.Equal("Low", result.BudgetAuthorizationLevel);
        Assert.Equal(135m, result.BudgetAvailableAmount);
        Assert.Null(result.AppliedIntensity);
        Assert.False(state.PendingUtilityIncidentResponse.IsScheduled);
        Assert.Empty(outboxWriter.Expenses);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, client.AuthorizeCallCount);
        Assert.Equal(0, tripDispatcher.DispatchCallCount);
        Assert.NotNull(client.LastRequest);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, client.LastRequest!.CityId);
        Assert.Equal("Operations", client.LastRequest.Category);
        Assert.Equal("UtilityIncidentResponseDispatch", client.LastRequest.OperationKind);
        Assert.Equal("Heavy", client.LastRequest.RequestedIntensity);
        Assert.Equal(
            CityMaintenanceOperationalExpenseFactory.EstimateUtilityIncidentResponseAmount(
                focus: "PowerOutages",
                intensity: "Heavy",
                districtFocused: true),
            client.LastRequest.EstimatedAmount);
    }

    [Fact]
    public async Task Handle_WhenDispatchIsApplied_SchedulesWorkWritesExpenseAndDispatchesTrip()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var client = new FakeCityBudgetAuthorizationClient();
        var tripDispatcher = new FakeCityOperationalTripDispatcher();
        var timeProvider = SimulationSystemsApplicationTestSupport.CreateTimeProvider(
            SimulationSystemsApplicationTestSupport.LaterUtc.AddHours(10));
        var handler = CreateHandler(repository, unitOfWork, outboxWriter, client, tripDispatcher, timeProvider);

        var result = await handler.Handle(
            CreateCommand(focus: "WaterLeaks", intensity: "Standard", focusDistrictId: FocusDistrictId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NotRequired", result!.BudgetAuthorizationStatus);
        Assert.Equal("Standard", result.RequestedIntensity);
        Assert.Equal("Standard", result.AppliedIntensity);
        Assert.Equal("Standard", result.BudgetAuthorizedIntensity);
        Assert.Equal(FocusDistrictId, result.FocusDistrictId);
        Assert.Equal(0, client.AuthorizeCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.True(state.PendingUtilityIncidentResponse.IsScheduled);
        Assert.Equal("WaterLeaks", state.PendingUtilityIncidentResponse.Focus);
        Assert.Equal("Standard", state.PendingUtilityIncidentResponse.Intensity);
        Assert.Equal(FocusDistrictId, state.PendingUtilityIncidentResponse.FocusDistrictId);
        Assert.Equal(1, state.PendingUtilityIncidentResponse.ReadyAtTickId);
        Assert.Single(outboxWriter.Expenses);
        Assert.Equal(timeProvider.GetUtcNow(), outboxWriter.Expenses[0].OccurredAtUtc);
        Assert.Equal(
            CityMaintenanceOperationalExpenseFactory.EstimateUtilityIncidentResponseAmount(
                focus: "WaterLeaks",
                intensity: "Standard",
                districtFocused: true),
            outboxWriter.Expenses[0].Amount);
        Assert.Equal(1, tripDispatcher.DispatchCallCount);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, tripDispatcher.CityId);
        Assert.Equal(FocusDistrictId, tripDispatcher.FocusDistrictId);
        Assert.Equal("WaterLeaks", tripDispatcher.Focus);
        Assert.Equal("Standard", tripDispatcher.Intensity);
    }

    private static DispatchCityUtilityIncidentResponseCommandHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeUnitOfWork unitOfWork,
        FakeCityOperationalExpenseOutboxWriter outboxWriter,
        FakeCityBudgetAuthorizationClient client,
        FakeCityOperationalTripDispatcher tripDispatcher,
        FrozenTimeProvider timeProvider)
    {
        return new DispatchCityUtilityIncidentResponseCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new ClassicCityWeatherPressureProfileFactory(),
            new CityMaintenanceBudgetGuard(),
            new CityMaintenanceBudgetAuthorizationService(client),
            tripDispatcher,
            timeProvider);
    }

    private static DispatchCityUtilityIncidentResponseCommand CreateCommand(
        string focus = "Balanced",
        string intensity = "Standard",
        bool emergencyOverride = false,
        Guid? focusDistrictId = null)
    {
        return new DispatchCityUtilityIncidentResponseCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            Focus: focus,
            Intensity: intensity,
            EmergencyOverride: emergencyOverride,
            FocusDistrictId: focusDistrictId);
    }
}

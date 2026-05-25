using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    DispatchCityUtilityIncidentResponse;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    DispatchCityUtilityIncidentResponse
{
    public sealed class DispatchCityUtilityIncidentResponseCommandHandlerTests
    {
        private static readonly Guid FocusDistrictId = Guid.Parse("74000000-0000-0000-0000-000000000001");

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            DispatchCityUtilityIncidentResponseCommandHandler handler = CreateHandler(
                repository: new FakeCityEnvironmentalConditionRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
                client: new FakeCityBudgetAuthorizationClient(),
                tripDispatcher: new FakeCityOperationalTripDispatcher(),
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WhenAuthorizationIsDenied_ReturnsBudgetDecisionWithoutMutations()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
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
            DispatchCityUtilityIncidentResponseCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                tripDispatcher: tripDispatcher,
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "PowerOutages",
                    intensity: "Heavy",
                    focusDistrictId: FocusDistrictId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: "Denied",
                actual: result!.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: "Low",
                actual: result.BudgetAuthorizationLevel);
            Assert.Equal(
                expected: 135m,
                actual: result.BudgetAvailableAmount);
            Assert.Null(result.AppliedIntensity);
            Assert.False(state.PendingUtilityIncidentResponse.IsScheduled);
            Assert.Empty(outboxWriter.Expenses);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 1,
                actual: client.AuthorizeCallCount);
            Assert.Equal(
                expected: 0,
                actual: tripDispatcher.DispatchCallCount);
            Assert.NotNull(client.LastRequest);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: client.LastRequest!.CityId);
            Assert.Equal(
                expected: "Operations",
                actual: client.LastRequest.Category);
            Assert.Equal(
                expected: "UtilityIncidentResponseDispatch",
                actual: client.LastRequest.OperationKind);
            Assert.Equal(
                expected: "Heavy",
                actual: client.LastRequest.RequestedIntensity);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateUtilityIncidentResponseAmount(
                    focus: "PowerOutages",
                    intensity: "Heavy",
                    districtFocused: true),
                actual: client.LastRequest.EstimatedAmount);
        }

        [Fact]
        public async Task Handle_WhenDispatchIsApplied_SchedulesWorkWritesExpenseAndDispatchesTrip()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityOperationalExpenseOutboxWriter();
            var client = new FakeCityBudgetAuthorizationClient();
            var tripDispatcher = new FakeCityOperationalTripDispatcher();
            FrozenTimeProvider timeProvider = SimulationSystemsApplicationTestSupport.CreateTimeProvider(
                SimulationSystemsApplicationTestSupport.LaterUtc.AddHours(10));
            DispatchCityUtilityIncidentResponseCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                tripDispatcher: tripDispatcher,
                timeProvider: timeProvider);

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "WaterLeaks",
                    intensity: "Standard",
                    focusDistrictId: FocusDistrictId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: "NotRequired",
                actual: result!.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: "Standard",
                actual: result.RequestedIntensity);
            Assert.Equal(
                expected: "Standard",
                actual: result.AppliedIntensity);
            Assert.Equal(
                expected: "Standard",
                actual: result.BudgetAuthorizedIntensity);
            Assert.Equal(
                expected: FocusDistrictId,
                actual: result.FocusDistrictId);
            Assert.Equal(
                expected: 0,
                actual: client.AuthorizeCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.True(state.PendingUtilityIncidentResponse.IsScheduled);
            Assert.Equal(
                expected: "WaterLeaks",
                actual: state.PendingUtilityIncidentResponse.Focus);
            Assert.Equal(
                expected: "Standard",
                actual: state.PendingUtilityIncidentResponse.Intensity);
            Assert.Equal(
                expected: FocusDistrictId,
                actual: state.PendingUtilityIncidentResponse.FocusDistrictId);
            Assert.Equal(
                expected: 1,
                actual: state.PendingUtilityIncidentResponse.ReadyAtTickId);
            Assert.Single(outboxWriter.Expenses);
            Assert.Equal(
                expected: timeProvider.GetUtcNow(),
                actual: outboxWriter.Expenses[0].OccurredAtUtc);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateUtilityIncidentResponseAmount(
                    focus: "WaterLeaks",
                    intensity: "Standard",
                    districtFocused: true),
                actual: outboxWriter.Expenses[0].Amount);
            Assert.Equal(
                expected: 1,
                actual: tripDispatcher.DispatchCallCount);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: tripDispatcher.CityId);
            Assert.Equal(
                expected: FocusDistrictId,
                actual: tripDispatcher.FocusDistrictId);
            Assert.Equal(
                expected: "WaterLeaks",
                actual: tripDispatcher.Focus);
            Assert.Equal(
                expected: "Standard",
                actual: tripDispatcher.Intensity);
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
                repository: repository,
                unitOfWork: unitOfWork,
                operationalExpenseOutboxWriter: outboxWriter,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                budgetGuard: new CityMaintenanceBudgetGuard(),
                budgetAuthorizationService: new CityMaintenanceBudgetAuthorizationService(client),
                operationalTripDispatcher: tripDispatcher,
                timeProvider: timeProvider);
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
}

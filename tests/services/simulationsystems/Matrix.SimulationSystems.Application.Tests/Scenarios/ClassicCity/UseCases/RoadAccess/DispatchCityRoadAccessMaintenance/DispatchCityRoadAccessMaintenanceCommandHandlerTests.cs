using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.
    DispatchCityRoadAccessMaintenance
{
    public sealed class DispatchCityRoadAccessMaintenanceCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            DispatchCityRoadAccessMaintenanceCommandHandler handler = CreateHandler(
                repository: new FakeCityEnvironmentalConditionRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
                client: new FakeCityBudgetAuthorizationClient(),
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CityRoadAccessStatusDto? result = await handler.Handle(
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
                    AvailableAmount: 120m,
                    PressureIndex: 0.73m,
                    EmergencyOverrideRequested: false,
                    AuthorizedByEmergencyOverride: false,
                    Summary: "Budget pressure blocks heavy road maintenance.")
            };
            DispatchCityRoadAccessMaintenanceCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CityRoadAccessStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "CorridorClearance",
                    intensity: "Heavy"),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: "Denied",
                actual: result!.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: "Low",
                actual: result.BudgetAuthorizationLevel);
            Assert.Equal(
                expected: 120m,
                actual: result.BudgetAvailableAmount);
            Assert.Null(result.AppliedIntensity);
            Assert.False(state.PendingRoadAccessMaintenance.IsScheduled);
            Assert.Empty(outboxWriter.Expenses);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 1,
                actual: client.AuthorizeCallCount);
            Assert.NotNull(client.LastRequest);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: client.LastRequest!.CityId);
            Assert.Equal(
                expected: "Infrastructure",
                actual: client.LastRequest.Category);
            Assert.Equal(
                expected: "RoadAccessMaintenanceDispatch",
                actual: client.LastRequest.OperationKind);
            Assert.Equal(
                expected: "Heavy",
                actual: client.LastRequest.RequestedIntensity);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                    systemName: "RoadAccess",
                    focus: "CorridorClearance",
                    intensity: "Heavy"),
                actual: client.LastRequest.EstimatedAmount);
        }

        [Fact]
        public async Task Handle_WhenDispatchIsApplied_SchedulesWorkAndWritesExpense()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityOperationalExpenseOutboxWriter();
            var client = new FakeCityBudgetAuthorizationClient();
            FrozenTimeProvider timeProvider = SimulationSystemsApplicationTestSupport.CreateTimeProvider(
                SimulationSystemsApplicationTestSupport.LaterUtc.AddHours(11));
            DispatchCityRoadAccessMaintenanceCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                timeProvider: timeProvider);

            CityRoadAccessStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "TrafficControl",
                    intensity: "Standard"),
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
                expected: 0,
                actual: client.AuthorizeCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.True(state.PendingRoadAccessMaintenance.IsScheduled);
            Assert.Equal(
                expected: "TrafficControl",
                actual: state.PendingRoadAccessMaintenance.Focus);
            Assert.Equal(
                expected: "Standard",
                actual: state.PendingRoadAccessMaintenance.Intensity);
            Assert.Equal(
                expected: 1,
                actual: state.PendingRoadAccessMaintenance.ReadyAtTickId);
            Assert.Single(outboxWriter.Expenses);
            Assert.Equal(
                expected: timeProvider.GetUtcNow(),
                actual: outboxWriter.Expenses[0].OccurredAtUtc);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                    systemName: "RoadAccess",
                    focus: "TrafficControl",
                    intensity: "Standard"),
                actual: outboxWriter.Expenses[0].Amount);
        }

        private static DispatchCityRoadAccessMaintenanceCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork unitOfWork,
            FakeCityOperationalExpenseOutboxWriter outboxWriter,
            FakeCityBudgetAuthorizationClient client,
            FrozenTimeProvider timeProvider)
        {
            return new DispatchCityRoadAccessMaintenanceCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                operationalExpenseOutboxWriter: outboxWriter,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                budgetGuard: new CityMaintenanceBudgetGuard(),
                budgetAuthorizationService: new CityMaintenanceBudgetAuthorizationService(client),
                timeProvider: timeProvider);
        }

        private static DispatchCityRoadAccessMaintenanceCommand CreateCommand(
            string focus = "Balanced",
            string intensity = "Standard",
            bool emergencyOverride = false)
        {
            return new DispatchCityRoadAccessMaintenanceCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Focus: focus,
                Intensity: intensity,
                EmergencyOverride: emergencyOverride);
        }
    }
}

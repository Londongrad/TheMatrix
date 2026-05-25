using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Sanitation.
    DispatchCitySanitationMaintenance
{
    public sealed class DispatchCitySanitationMaintenanceCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            DispatchCitySanitationMaintenanceCommandHandler handler = CreateHandler(
                repository: new FakeCityEnvironmentalConditionRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
                client: new FakeCityBudgetAuthorizationClient(),
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CitySanitationStatusDto? result = await handler.Handle(
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
                    AvailableAmount: 110m,
                    PressureIndex: 0.72m,
                    EmergencyOverrideRequested: false,
                    AuthorizedByEmergencyOverride: false,
                    Summary: "Budget pressure blocks heavy sanitation maintenance.")
            };
            DispatchCitySanitationMaintenanceCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CitySanitationStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "OverflowControl",
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
                expected: 110m,
                actual: result.BudgetAvailableAmount);
            Assert.Null(result.AppliedIntensity);
            Assert.False(state.PendingSanitationMaintenance.IsScheduled);
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
                expected: "SanitationMaintenanceDispatch",
                actual: client.LastRequest.OperationKind);
            Assert.Equal(
                expected: "Heavy",
                actual: client.LastRequest.RequestedIntensity);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                    systemName: "Sanitation",
                    focus: "OverflowControl",
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
                SimulationSystemsApplicationTestSupport.LaterUtc.AddHours(7));
            DispatchCitySanitationMaintenanceCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                timeProvider: timeProvider);

            CitySanitationStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "SewerRepairs",
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
            Assert.True(state.PendingSanitationMaintenance.IsScheduled);
            Assert.Equal(
                expected: "SewerRepairs",
                actual: state.PendingSanitationMaintenance.Focus);
            Assert.Equal(
                expected: "Standard",
                actual: state.PendingSanitationMaintenance.Intensity);
            Assert.Equal(
                expected: 1,
                actual: state.PendingSanitationMaintenance.ReadyAtTickId);
            Assert.Single(outboxWriter.Expenses);
            Assert.Equal(
                expected: timeProvider.GetUtcNow(),
                actual: outboxWriter.Expenses[0].OccurredAtUtc);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                    systemName: "Sanitation",
                    focus: "SewerRepairs",
                    intensity: "Standard"),
                actual: outboxWriter.Expenses[0].Amount);
        }

        private static DispatchCitySanitationMaintenanceCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork unitOfWork,
            FakeCityOperationalExpenseOutboxWriter outboxWriter,
            FakeCityBudgetAuthorizationClient client,
            FrozenTimeProvider timeProvider)
        {
            return new DispatchCitySanitationMaintenanceCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                operationalExpenseOutboxWriter: outboxWriter,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                budgetGuard: new CityMaintenanceBudgetGuard(),
                budgetAuthorizationService: new CityMaintenanceBudgetAuthorizationService(client),
                timeProvider: timeProvider);
        }

        private static DispatchCitySanitationMaintenanceCommand CreateCommand(
            string focus = "Balanced",
            string intensity = "Standard",
            bool emergencyOverride = false)
        {
            return new DispatchCitySanitationMaintenanceCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Focus: focus,
                Intensity: intensity,
                EmergencyOverride: emergencyOverride);
        }
    }
}

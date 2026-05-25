using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    DispatchCityPowerDistributionMaintenance;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.PowerDistribution.
    DispatchCityPowerDistributionMaintenance
{
    public sealed class DispatchCityPowerDistributionMaintenanceCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            DispatchCityPowerDistributionMaintenanceCommandHandler handler = CreateHandler(
                repository: new FakeCityEnvironmentalConditionRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
                client: new FakeCityBudgetAuthorizationClient(),
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CityPowerDistributionStatusDto? result = await handler.Handle(
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
                    AvailableAmount: 125m,
                    PressureIndex: 0.74m,
                    EmergencyOverrideRequested: false,
                    AuthorizedByEmergencyOverride: false,
                    Summary: "Budget pressure blocks heavy power maintenance.")
            };
            DispatchCityPowerDistributionMaintenanceCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                timeProvider: SimulationSystemsApplicationTestSupport.CreateTimeProvider());

            CityPowerDistributionStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "GridStabilization",
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
                expected: 125m,
                actual: result.BudgetAvailableAmount);
            Assert.Null(result.AppliedIntensity);
            Assert.False(state.PendingPowerDistributionMaintenance.IsScheduled);
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
                expected: "PowerDistributionMaintenanceDispatch",
                actual: client.LastRequest.OperationKind);
            Assert.Equal(
                expected: "Heavy",
                actual: client.LastRequest.RequestedIntensity);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                    systemName: "PowerDistribution",
                    focus: "GridStabilization",
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
                SimulationSystemsApplicationTestSupport.LaterUtc.AddHours(8));
            DispatchCityPowerDistributionMaintenanceCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                client: client,
                timeProvider: timeProvider);

            CityPowerDistributionStatusDto? result = await handler.Handle(
                request: CreateCommand(
                    focus: "SwitchingRecovery",
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
            Assert.True(state.PendingPowerDistributionMaintenance.IsScheduled);
            Assert.Equal(
                expected: "SwitchingRecovery",
                actual: state.PendingPowerDistributionMaintenance.Focus);
            Assert.Equal(
                expected: "Standard",
                actual: state.PendingPowerDistributionMaintenance.Intensity);
            Assert.Equal(
                expected: 1,
                actual: state.PendingPowerDistributionMaintenance.ReadyAtTickId);
            Assert.Single(outboxWriter.Expenses);
            Assert.Equal(
                expected: timeProvider.GetUtcNow(),
                actual: outboxWriter.Expenses[0].OccurredAtUtc);
            Assert.Equal(
                expected: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                    systemName: "PowerDistribution",
                    focus: "SwitchingRecovery",
                    intensity: "Standard"),
                actual: outboxWriter.Expenses[0].Amount);
        }

        private static DispatchCityPowerDistributionMaintenanceCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork unitOfWork,
            FakeCityOperationalExpenseOutboxWriter outboxWriter,
            FakeCityBudgetAuthorizationClient client,
            FrozenTimeProvider timeProvider)
        {
            return new DispatchCityPowerDistributionMaintenanceCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                operationalExpenseOutboxWriter: outboxWriter,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                budgetGuard: new CityMaintenanceBudgetGuard(),
                budgetAuthorizationService: new CityMaintenanceBudgetAuthorizationService(client),
                timeProvider: timeProvider);
        }

        private static DispatchCityPowerDistributionMaintenanceCommand CreateCommand(
            string focus = "Balanced",
            string intensity = "Standard",
            bool emergencyOverride = false)
        {
            return new DispatchCityPowerDistributionMaintenanceCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Focus: focus,
                Intensity: intensity,
                EmergencyOverride: emergencyOverride);
        }
    }
}

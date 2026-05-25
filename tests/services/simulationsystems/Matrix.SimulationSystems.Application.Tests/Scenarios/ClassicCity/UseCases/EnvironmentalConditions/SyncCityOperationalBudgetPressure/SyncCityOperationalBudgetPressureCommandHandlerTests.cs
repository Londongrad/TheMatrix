using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityOperationalBudgetPressure;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.NotInitialized,
                actual: result.Status);
        }

        [Fact]
        public async Task Handle_WhenSnapshotIsStale_ReturnsStale()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.ApplyOperationalBudgetPressure(
                new CityOperationalBudgetPressureSnapshot(
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
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 6,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0.10m,
                actual: result.PressureIndex);
        }

        [Fact]
        public async Task Handle_WhenSnapshotApplies_ReturnsApplied()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 5,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 0.72m,
                actual: state.OperationalBudgetPressure.PressureIndex);
            Assert.Equal(
                expected: 5,
                actual: state.OperationalBudgetPressure.EffectiveTickId);
        }

        [Fact]
        public async Task Handle_WhenConcurrencyPersistsMatchingSnapshot_ReturnsConcurrent()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork
            {
                SaveException = new DbUpdateConcurrencyException("race")
            };
            var handler = new SyncCityOperationalBudgetPressureCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            SyncCityOperationalBudgetPressureResult result = await handler.Handle(
                request: CreateCommand(
                    effectiveTickId: 5,
                    effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SyncCityOperationalBudgetPressureStatus.Concurrent,
                actual: result.Status);
            Assert.Equal(
                expected: 0.72m,
                actual: result.PressureIndex);
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
}

using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    SetCityUtilityIncidentEmergencyMode;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    SetCityUtilityIncidentEmergencyMode
{
    public sealed class SetCityUtilityIncidentEmergencyModeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new SetCityUtilityIncidentEmergencyModeCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: new SetCityUtilityIncidentEmergencyModeCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CreateHostId(),
                actual: repository.RequestedSimulationHostId);
        }

        [Fact]
        public async Task Handle_WhenStateExists_TogglesEmergencyModeAndReturnsUpdatedDto()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            var handler = new SetCityUtilityIncidentEmergencyModeCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: new SetCityUtilityIncidentEmergencyModeCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(state.UtilityIncidentInfrastructure.EmergencyModeEnabled);
            Assert.True(result!.EmergencyModeEnabled);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.UtilityIncidentInfrastructure.DispatchReadinessIndex,
                actual: result.DispatchReadinessIndex);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}

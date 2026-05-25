using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.SetCityRoadAccessEmergencyMode;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.
    SetCityRoadAccessEmergencyMode
{
    public sealed class SetCityRoadAccessEmergencyModeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new SetCityRoadAccessEmergencyModeCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityRoadAccessStatusDto? result = await handler.Handle(
                request: new SetCityRoadAccessEmergencyModeCommand(
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
            var handler = new SetCityRoadAccessEmergencyModeCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityRoadAccessStatusDto? result = await handler.Handle(
                request: new SetCityRoadAccessEmergencyModeCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(state.RoadAccessInfrastructure.EmergencyModeEnabled);
            Assert.True(result!.EmergencyModeEnabled);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.RoadAccessInfrastructure.CorridorAvailabilityIndex,
                actual: result.CorridorAvailabilityIndex);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}

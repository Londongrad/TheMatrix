using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    SetCityPowerDistributionEmergencyMode;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.PowerDistribution.
    SetCityPowerDistributionEmergencyMode
{
    public sealed class SetCityPowerDistributionEmergencyModeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new SetCityPowerDistributionEmergencyModeCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityPowerDistributionStatusDto? result = await handler.Handle(
                request: new SetCityPowerDistributionEmergencyModeCommand(
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
            var handler = new SetCityPowerDistributionEmergencyModeCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityPowerDistributionStatusDto? result = await handler.Handle(
                request: new SetCityPowerDistributionEmergencyModeCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(state.PowerDistributionInfrastructure.EmergencyModeEnabled);
            Assert.True(result!.EmergencyModeEnabled);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.PowerDistributionInfrastructure.SubstationCapacityIndex,
                actual: result.SubstationCapacityIndex);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}

using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityPowerDistributionStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityPowerDistributionStatus
{
    public sealed class GetCityPowerDistributionStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCityPowerDistributionStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityPowerDistributionStatusDto? result = await handler.Handle(
                request: new GetCityPowerDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CreateHostId(),
                actual: repository.RequestedSimulationHostId);
        }

        [Fact]
        public async Task Handle_WhenStateExists_ReturnsMappedDto()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.ApplyWeatherPressure(
                new CityWeatherPressureProfile(
                    rainPressure: 0.19m,
                    snowPressure: 0.14m,
                    stormPressure: 0.33m,
                    freezePressure: 0.11m,
                    thawRelief: 0.07m));
            state.SchedulePowerDistributionMaintenance(
                focus: PowerDistributionMaintenanceFocus.SwitchingRecovery,
                intensity: PowerDistributionMaintenanceIntensity.Heavy,
                readyAtTickId: 17);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityPowerDistributionStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CityPowerDistributionStatusDto? result = await handler.Handle(
                request: new GetCityPowerDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.PowerCoverageIndex.Value,
                actual: result.PowerCoverageIndex);
            Assert.Equal(
                expected: state.PowerDistributionInfrastructure.SubstationCapacityIndex,
                actual: result.SubstationCapacityIndex);
            Assert.Equal(
                expected: state.PowerDistributionInfrastructure.GridIntegrityIndex,
                actual: result.GridIntegrityIndex);
            Assert.Equal(
                expected: state.PowerDistribution.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .PowerSupport,
                actual: result.PowerSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "SwitchingRecovery",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 17,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

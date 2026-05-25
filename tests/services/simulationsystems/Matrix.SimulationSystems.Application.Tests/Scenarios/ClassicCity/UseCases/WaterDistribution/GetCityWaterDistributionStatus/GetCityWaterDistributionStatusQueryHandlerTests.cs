using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityWaterDistributionStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityWaterDistributionStatus
{
    public sealed class GetCityWaterDistributionStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCityWaterDistributionStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityWaterDistributionStatusDto? result = await handler.Handle(
                request: new GetCityWaterDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    rainPressure: 0.31m,
                    snowPressure: 0.08m,
                    stormPressure: 0.27m,
                    freezePressure: 0.16m,
                    thawRelief: 0.10m));
            state.ScheduleWaterDistributionMaintenance(
                focus: WaterDistributionMaintenanceFocus.PumpRecovery,
                intensity: WaterDistributionMaintenanceIntensity.Heavy,
                readyAtTickId: 13);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityWaterDistributionStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CityWaterDistributionStatusDto? result = await handler.Handle(
                request: new GetCityWaterDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.WaterCoverageIndex.Value,
                actual: result.WaterCoverageIndex);
            Assert.Equal(
                expected: state.WaterDistributionInfrastructure.TreatmentCapacityIndex,
                actual: result.TreatmentCapacityIndex);
            Assert.Equal(
                expected: state.WaterDistributionInfrastructure.PumpReadinessIndex,
                actual: result.PumpReadinessIndex);
            Assert.Equal(
                expected: state.WaterDistribution.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .WaterSupport,
                actual: result.WaterSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "PumpRecovery",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 13,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

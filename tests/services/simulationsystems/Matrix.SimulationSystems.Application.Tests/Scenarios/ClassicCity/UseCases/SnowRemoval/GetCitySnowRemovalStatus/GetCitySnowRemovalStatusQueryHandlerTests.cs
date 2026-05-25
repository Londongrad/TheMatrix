using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus
{
    public sealed class GetCitySnowRemovalStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCitySnowRemovalStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CitySnowRemovalStatusDto? result = await handler.Handle(
                request: new GetCitySnowRemovalStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    rainPressure: 0.04m,
                    snowPressure: 0.42m,
                    stormPressure: 0.18m,
                    freezePressure: 0.29m,
                    thawRelief: 0.03m));
            state.ScheduleSnowRemovalMaintenance(
                focus: SnowRemovalMaintenanceFocus.RouteClearance,
                intensity: SnowRemovalMaintenanceIntensity.Heavy,
                readyAtTickId: 19);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCitySnowRemovalStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CitySnowRemovalStatusDto? result = await handler.Handle(
                request: new GetCitySnowRemovalStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.SnowAccumulationIndex.Value,
                actual: result.SnowAccumulationIndex);
            Assert.Equal(
                expected: state.SnowRemovalInfrastructure.FleetAvailabilityIndex,
                actual: result.FleetAvailabilityIndex);
            Assert.Equal(
                expected: state.SnowRemovalInfrastructure.RouteCoverageIndex,
                actual: result.RouteCoverageIndex);
            Assert.Equal(
                expected: state.SnowRemoval.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .SnowRemovalSupport,
                actual: result.SnowRemovalSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "RouteClearance",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 19,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

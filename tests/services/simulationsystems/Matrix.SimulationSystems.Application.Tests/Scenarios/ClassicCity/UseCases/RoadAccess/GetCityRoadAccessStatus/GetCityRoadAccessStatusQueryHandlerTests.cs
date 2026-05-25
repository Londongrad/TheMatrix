using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus
{
    public sealed class GetCityRoadAccessStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCityRoadAccessStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityRoadAccessStatusDto? result = await handler.Handle(
                request: new GetCityRoadAccessStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    rainPressure: 0.18m,
                    snowPressure: 0.34m,
                    stormPressure: 0.21m,
                    freezePressure: 0.24m,
                    thawRelief: 0.04m));
            state.ScheduleRoadAccessMaintenance(
                focus: RoadAccessMaintenanceFocus.CorridorClearance,
                intensity: RoadAccessMaintenanceIntensity.Heavy,
                readyAtTickId: 23);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityRoadAccessStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CityRoadAccessStatusDto? result = await handler.Handle(
                request: new GetCityRoadAccessStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.FloodingIndex.Value,
                actual: result.FloodingIndex);
            Assert.Equal(
                expected: state.RoadAccessibilityIndex.Value,
                actual: result.RoadAccessibilityIndex);
            Assert.Equal(
                expected: state.RoadAccessInfrastructure.CorridorAvailabilityIndex,
                actual: result.CorridorAvailabilityIndex);
            Assert.Equal(
                expected: state.RoadAccessInfrastructure.SurfaceIntegrityIndex,
                actual: result.SurfaceIntegrityIndex);
            Assert.Equal(
                expected: state.RoadAccess.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .RoadSupport,
                actual: result.RoadSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "CorridorClearance",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 23,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

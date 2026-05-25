using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus
{
    public sealed class GetCityHeatingStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCityHeatingStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityHeatingStatusDto? result = await handler.Handle(
                request: new GetCityHeatingStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    snowPressure: 0.22m,
                    stormPressure: 0.14m,
                    freezePressure: 0.47m,
                    thawRelief: 0.03m));
            state.ScheduleHeatingMaintenance(
                focus: HeatingMaintenanceFocus.PlantRepairs,
                intensity: HeatingMaintenanceIntensity.Heavy,
                readyAtTickId: 11);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityHeatingStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CityHeatingStatusDto? result = await handler.Handle(
                request: new GetCityHeatingStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.HeatingCoverageIndex.Value,
                actual: result.HeatingCoverageIndex);
            Assert.Equal(
                expected: state.HeatingInfrastructure.PlantCapacityIndex,
                actual: result.PlantCapacityIndex);
            Assert.Equal(
                expected: state.HeatingInfrastructure.ControlReadinessIndex,
                actual: result.ControlReadinessIndex);
            Assert.Equal(
                expected: state.Heating.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .HeatingSupport,
                actual: result.HeatingSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "PlantRepairs",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 11,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

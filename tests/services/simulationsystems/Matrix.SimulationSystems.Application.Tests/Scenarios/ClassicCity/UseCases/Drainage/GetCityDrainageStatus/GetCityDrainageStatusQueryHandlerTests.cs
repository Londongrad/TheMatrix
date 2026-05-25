using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.GetCityDrainageStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Drainage.GetCityDrainageStatus
{
    public sealed class GetCityDrainageStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCityDrainageStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityDrainageStatusDto? result = await handler.Handle(
                request: new GetCityDrainageStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    rainPressure: 0.36m,
                    snowPressure: 0.18m,
                    stormPressure: 0.44m,
                    freezePressure: 0.11m,
                    thawRelief: 0.05m));
            state.ScheduleDrainageMaintenance(
                focus: DrainageMaintenanceFocus.PumpRepairs,
                intensity: DrainageMaintenanceIntensity.Heavy,
                readyAtTickId: 9);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityDrainageStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CityDrainageStatusDto? result = await handler.Handle(
                request: new GetCityDrainageStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                expected: state.DrainageInfrastructure.PumpCapacityIndex,
                actual: result.PumpCapacityIndex);
            Assert.Equal(
                expected: state.DrainageInfrastructure.NetworkIntegrityIndex,
                actual: result.NetworkIntegrityIndex);
            Assert.Equal(
                expected: state.Drainage.LoadIndex,
                actual: result.System.LoadIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .DrainageSupport,
                actual: result.DrainageSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "PumpRepairs",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 9,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

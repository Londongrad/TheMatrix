using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus
{
    public sealed class GetCitySanitationStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCitySanitationStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CitySanitationStatusDto? result = await handler.Handle(
                request: new GetCitySanitationStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    rainPressure: 0.42m,
                    snowPressure: 0.04m,
                    stormPressure: 0.21m,
                    freezePressure: 0.10m,
                    thawRelief: 0.06m));
            state.ScheduleSanitationMaintenance(
                focus: SanitationMaintenanceFocus.OverflowControl,
                intensity: SanitationMaintenanceIntensity.Heavy,
                readyAtTickId: 15);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCitySanitationStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CitySanitationStatusDto? result = await handler.Handle(
                request: new GetCitySanitationStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.SanitationCoverageIndex.Value,
                actual: result.SanitationCoverageIndex);
            Assert.Equal(
                expected: state.SanitationInfrastructure.TreatmentStabilityIndex,
                actual: result.TreatmentStabilityIndex);
            Assert.Equal(
                expected: state.SanitationInfrastructure.OverflowControlIndex,
                actual: result.OverflowControlIndex);
            Assert.Equal(
                expected: state.Sanitation.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .SanitationSupport,
                actual: result.SanitationSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "OverflowControl",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 15,
                actual: result.PendingOperation.ReadyAtTickId);
        }
    }
}

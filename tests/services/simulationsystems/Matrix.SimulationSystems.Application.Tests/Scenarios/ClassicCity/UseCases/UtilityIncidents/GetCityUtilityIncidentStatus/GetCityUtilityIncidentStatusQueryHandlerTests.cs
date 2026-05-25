using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityUtilityIncidentStatus
{
    public sealed class GetCityUtilityIncidentStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var handler = new GetCityUtilityIncidentStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: new GetCityUtilityIncidentStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                    rainPressure: 0.23m,
                    snowPressure: 0.17m,
                    stormPressure: 0.35m,
                    freezePressure: 0.14m,
                    thawRelief: 0.05m));
            state.ScheduleUtilityIncidentResponse(
                focus: UtilityIncidentResponseFocus.PowerOutages,
                intensity: UtilityIncidentResponseIntensity.Heavy,
                focusDistrictId: Guid.Parse("74000000-0000-0000-0000-000000000001"),
                readyAtTickId: 21);

            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityUtilityIncidentStatusQueryHandler(
                repository: repository,
                pressureProfileFactory: profileFactory);

            CityUtilityIncidentStatusDto? result = await handler.Handle(
                request: new GetCityUtilityIncidentStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.UtilityContinuityIndex.Value,
                actual: result.UtilityContinuityIndex);
            Assert.Equal(
                expected: state.UtilityIncidentInfrastructure.DispatchReadinessIndex,
                actual: result.DispatchReadinessIndex);
            Assert.Equal(
                expected: state.UtilityIncidentInfrastructure.RestorationCoverageIndex,
                actual: result.RestorationCoverageIndex);
            Assert.Equal(
                expected: state.UtilityIncidents.ServiceQualityIndex,
                actual: result.System.ServiceQualityIndex);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .UtilityIncidentSupport,
                actual: result.UtilityIncidentSupportIndex);
            Assert.NotNull(result.PendingOperation);
            Assert.Equal(
                expected: "PowerOutages",
                actual: result.PendingOperation!.Focus);
            Assert.Equal(
                expected: "Heavy",
                actual: result.PendingOperation.Intensity);
            Assert.Equal(
                expected: 21,
                actual: result.PendingOperation.ReadyAtTickId);
            Assert.Equal(
                expected: Guid.Parse("74000000-0000-0000-0000-000000000001"),
                actual: result.FocusDistrictId);
        }
    }
}

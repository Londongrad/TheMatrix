using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed class GetCityRoadSegmentConditionsQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ICityMapTopologyClient cityMapTopologyClient,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        ClassicCityRoadSegmentConditionProjectionPolicy projectionPolicy)
        : IRequestHandler<GetCityRoadSegmentConditionsQuery, CityRoadSegmentConditionsDto?>
    {
        public async Task<CityRoadSegmentConditionsDto?> Handle(
            GetCityRoadSegmentConditionsQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            CityRoadGraphTopologyDto? topology = await cityMapTopologyClient.GetRoadGraphAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            if (topology is null)
                return null;

            decimal roadSupportIndex = pressureProfileFactory.Create(state).RoadSupport;
            IReadOnlyList<CityRoadSegmentConditionDto> segments = projectionPolicy.Project(
                topology: topology,
                state: state,
                roadSupportIndex: roadSupportIndex);

            return new CityRoadSegmentConditionsDto(
                CityId: request.CityId,
                EffectiveTickId: state.LastAppliedTickId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                RoadSupportIndex: roadSupportIndex,
                Segments: segments);
        }
    }
}

using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions
{
    public sealed class GetCityDistrictHeatingConditionsQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ICityMapTopologyClient cityMapTopologyClient,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        ClassicCityDistrictHeatingProjectionPolicy projectionPolicy)
        : IRequestHandler<GetCityDistrictHeatingConditionsQuery, CityDistrictHeatingConditionsDto?>
    {
        public async Task<CityDistrictHeatingConditionsDto?> Handle(
            GetCityDistrictHeatingConditionsQuery request,
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

            decimal heatingSupportIndex = pressureProfileFactory.Create(state)
               .HeatingSupport;
            IReadOnlyList<CityDistrictHeatingConditionDto> districts = projectionPolicy.Project(
                topology: topology,
                state: state,
                heatingSupportIndex: heatingSupportIndex);

            return new CityDistrictHeatingConditionsDto(
                CityId: request.CityId,
                EffectiveTickId: state.LastAppliedTickId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                HeatingSupportIndex: heatingSupportIndex,
                Districts: districts);
        }
    }
}

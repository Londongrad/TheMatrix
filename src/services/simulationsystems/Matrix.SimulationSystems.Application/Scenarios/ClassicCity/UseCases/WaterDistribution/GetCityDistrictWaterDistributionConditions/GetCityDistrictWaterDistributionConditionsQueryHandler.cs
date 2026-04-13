using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityDistrictWaterDistributionConditions
{
    public sealed class GetCityDistrictWaterDistributionConditionsQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ICityMapTopologyClient cityMapTopologyClient,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        ClassicCityDistrictWaterDistributionProjectionPolicy projectionPolicy)
        : IRequestHandler<GetCityDistrictWaterDistributionConditionsQuery, CityDistrictWaterDistributionConditionsDto?>
    {
        public async Task<CityDistrictWaterDistributionConditionsDto?> Handle(
            GetCityDistrictWaterDistributionConditionsQuery request,
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

            decimal waterSupportIndex = pressureProfileFactory.Create(state).WaterSupport;
            IReadOnlyList<CityDistrictWaterDistributionConditionDto> districts = projectionPolicy.Project(
                topology: topology,
                state: state,
                waterSupportIndex: waterSupportIndex);

            return new CityDistrictWaterDistributionConditionsDto(
                CityId: request.CityId,
                EffectiveTickId: state.LastAppliedTickId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                WaterSupportIndex: waterSupportIndex,
                Districts: districts);
        }
    }
}

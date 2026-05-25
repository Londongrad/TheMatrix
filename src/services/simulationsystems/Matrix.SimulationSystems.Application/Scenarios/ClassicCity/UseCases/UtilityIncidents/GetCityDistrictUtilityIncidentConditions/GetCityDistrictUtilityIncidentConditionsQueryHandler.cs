using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityDistrictWaterDistributionConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions
{
    public sealed class GetCityDistrictUtilityIncidentConditionsQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ICityMapTopologyClient cityMapTopologyClient,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        ClassicCityDistrictHeatingProjectionPolicy heatingProjectionPolicy,
        ClassicCityDistrictWaterDistributionProjectionPolicy waterProjectionPolicy,
        ClassicCityDistrictPowerDistributionProjectionPolicy powerProjectionPolicy,
        ClassicCityDistrictSanitationProjectionPolicy sanitationProjectionPolicy,
        ClassicCityDistrictUtilityIncidentProjectionPolicy projectionPolicy)
        : IRequestHandler<GetCityDistrictUtilityIncidentConditionsQuery, CityDistrictUtilityIncidentConditionsDto?>
    {
        public async Task<CityDistrictUtilityIncidentConditionsDto?> Handle(
            GetCityDistrictUtilityIncidentConditionsQuery request,
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

            CitySystemPressureProfile pressureProfile = pressureProfileFactory.Create(state);
            IReadOnlyDictionary<Guid, CityDistrictHeatingConditionDto> heatingByDistrictId = heatingProjectionPolicy
               .Project(
                    topology: topology,
                    state: state,
                    heatingSupportIndex: pressureProfile.HeatingSupport)
               .ToDictionary(x => x.DistrictId);
            IReadOnlyDictionary<Guid, CityDistrictWaterDistributionConditionDto> waterByDistrictId =
                waterProjectionPolicy.Project(
                        topology: topology,
                        state: state,
                        waterSupportIndex: pressureProfile.WaterSupport)
                   .ToDictionary(x => x.DistrictId);
            IReadOnlyDictionary<Guid, CityDistrictPowerDistributionConditionDto> powerByDistrictId =
                powerProjectionPolicy.Project(
                        topology: topology,
                        state: state,
                        powerSupportIndex: pressureProfile.PowerSupport)
                   .ToDictionary(x => x.DistrictId);
            IReadOnlyDictionary<Guid, CityDistrictSanitationConditionDto> sanitationByDistrictId =
                sanitationProjectionPolicy.Project(
                        topology: topology,
                        state: state,
                        sanitationSupportIndex: pressureProfile.SanitationSupport)
                   .ToDictionary(x => x.DistrictId);

            IReadOnlyList<CityDistrictUtilityIncidentConditionDto> districts = projectionPolicy.Project(
                topology: topology,
                state: state,
                utilityIncidentSupportIndex: pressureProfile.UtilityIncidentSupport,
                heatingByDistrictId: heatingByDistrictId,
                waterByDistrictId: waterByDistrictId,
                powerByDistrictId: powerByDistrictId,
                sanitationByDistrictId: sanitationByDistrictId);

            return new CityDistrictUtilityIncidentConditionsDto(
                CityId: request.CityId,
                EffectiveTickId: state.LastAppliedTickId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                UtilityIncidentSupportIndex: pressureProfile.UtilityIncidentSupport,
                Districts: districts);
        }
    }
}

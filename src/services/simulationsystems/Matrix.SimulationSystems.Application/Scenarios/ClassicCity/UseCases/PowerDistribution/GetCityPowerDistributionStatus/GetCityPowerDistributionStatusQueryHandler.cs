using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityPowerDistributionStatus
{
    public sealed class GetCityPowerDistributionStatusQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<GetCityPowerDistributionStatusQuery, CityPowerDistributionStatusDto?>
    {
        public async Task<CityPowerDistributionStatusDto?> Handle(
            GetCityPowerDistributionStatusQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            decimal powerSupport = pressureProfileFactory.Create(state)
               .PowerSupport;

            return CityPowerDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                powerSupportIndex: powerSupport);
        }
    }
}

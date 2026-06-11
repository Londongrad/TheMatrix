using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus
{
    public sealed class GetCitySnowRemovalStatusQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<GetCitySnowRemovalStatusQuery, CitySnowRemovalStatusDto?>
    {
        public async Task<CitySnowRemovalStatusDto?> Handle(
            GetCitySnowRemovalStatusQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            decimal snowRemovalSupport = pressureProfileFactory.Create(state)
               .SnowRemovalSupport;

            return CitySnowRemovalStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                snowRemovalSupportIndex: snowRemovalSupport);
        }
    }
}

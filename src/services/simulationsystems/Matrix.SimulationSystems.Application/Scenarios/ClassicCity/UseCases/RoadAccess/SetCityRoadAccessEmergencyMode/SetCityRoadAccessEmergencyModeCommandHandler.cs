using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.SetCityRoadAccessEmergencyMode
{
    public sealed class SetCityRoadAccessEmergencyModeCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SetCityRoadAccessEmergencyModeCommand, CityRoadAccessStatusDto?>
    {
        public async Task<CityRoadAccessStatusDto?> Handle(
            SetCityRoadAccessEmergencyModeCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            state.SetRoadAccessEmergencyMode(request.Enabled);

            CityEnvironmentalConditionSnapshot refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal roadSupport = pressureProfileFactory.Create(state)
               .RoadSupport;

            return CityRoadAccessStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                roadSupportIndex: roadSupport);
        }
    }
}

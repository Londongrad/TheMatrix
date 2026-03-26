using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.SetCityUtilityIncidentEmergencyMode
{
    public sealed class SetCityUtilityIncidentEmergencyModeCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SetCityUtilityIncidentEmergencyModeCommand, CityUtilityIncidentStatusDto?>
    {
        public async Task<CityUtilityIncidentStatusDto?> Handle(
            SetCityUtilityIncidentEmergencyModeCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            state.SetUtilityIncidentEmergencyMode(request.Enabled);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal utilityIncidentSupport = pressureProfileFactory.Create(state).UtilityIncidentSupport;

            return CityUtilityIncidentStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                utilityIncidentSupportIndex: utilityIncidentSupport);
        }
    }
}

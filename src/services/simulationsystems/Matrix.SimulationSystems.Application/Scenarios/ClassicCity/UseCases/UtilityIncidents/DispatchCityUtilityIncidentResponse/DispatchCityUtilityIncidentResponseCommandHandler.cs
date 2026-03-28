using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse
{
    public sealed class DispatchCityUtilityIncidentResponseCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<DispatchCityUtilityIncidentResponseCommand, CityUtilityIncidentStatusDto?>
    {
        public async Task<CityUtilityIncidentStatusDto?> Handle(
            DispatchCityUtilityIncidentResponseCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            UtilityIncidentResponseFocus focus = Enum.Parse<UtilityIncidentResponseFocus>(
                value: request.Focus,
                ignoreCase: true);
            UtilityIncidentResponseIntensity intensity = Enum.Parse<UtilityIncidentResponseIntensity>(
                value: request.Intensity,
                ignoreCase: true);

            state.DispatchUtilityIncidentResponse(
                focus: focus,
                intensity: intensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateUtilityIncidentResponseExpense(
                    cityId: request.CityId,
                    focus: request.Focus,
                    intensity: request.Intensity,
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal utilityIncidentSupport = pressureProfileFactory.Create(state).UtilityIncidentSupport;

            return CityUtilityIncidentStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                utilityIncidentSupportIndex: utilityIncidentSupport);
        }
    }
}

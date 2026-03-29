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
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard)
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
            UtilityIncidentResponseIntensity requestedIntensity = Enum.Parse<UtilityIncidentResponseIntensity>(
                value: request.Intensity,
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: requestedIntensity.ToString(),
                budget: state.OperationalBudgetPressure.ToSnapshot(),
                emergencyModeEnabled: state.UtilityIncidentInfrastructure.EmergencyModeEnabled);
            UtilityIncidentResponseIntensity appliedIntensity = Enum.Parse<UtilityIncidentResponseIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.DispatchUtilityIncidentResponse(
                focus: focus,
                intensity: appliedIntensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateUtilityIncidentResponseExpense(
                    cityId: request.CityId,
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal utilityIncidentSupport = pressureProfileFactory.Create(state).UtilityIncidentSupport;

            return CityUtilityIncidentStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                utilityIncidentSupportIndex: utilityIncidentSupport,
                requestedIntensity: budgetDecision.RequestedIntensity,
                appliedIntensity: budgetDecision.AppliedIntensity);
        }
    }
}

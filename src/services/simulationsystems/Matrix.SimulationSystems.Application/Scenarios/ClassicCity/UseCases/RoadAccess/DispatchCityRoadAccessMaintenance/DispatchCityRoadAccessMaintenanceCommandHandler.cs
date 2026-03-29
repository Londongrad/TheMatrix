using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance
{
    public sealed class DispatchCityRoadAccessMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard)
        : IRequestHandler<DispatchCityRoadAccessMaintenanceCommand, CityRoadAccessStatusDto?>
    {
        public async Task<CityRoadAccessStatusDto?> Handle(
            DispatchCityRoadAccessMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            RoadAccessMaintenanceFocus focus = Enum.Parse<RoadAccessMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            RoadAccessMaintenanceIntensity requestedIntensity = Enum.Parse<RoadAccessMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: requestedIntensity.ToString(),
                budget: state.OperationalBudgetPressure.ToSnapshot(),
                emergencyModeEnabled: state.RoadAccessInfrastructure.EmergencyModeEnabled);
            RoadAccessMaintenanceIntensity appliedIntensity = Enum.Parse<RoadAccessMaintenanceIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.DispatchRoadAccessMaintenance(
                focus: focus,
                intensity: appliedIntensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "RoadAccess",
                    operationKind: "RoadAccessMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal roadSupport = pressureProfileFactory.Create(state).RoadSupport;

            return CityRoadAccessStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                roadSupportIndex: roadSupport,
                requestedIntensity: budgetDecision.RequestedIntensity,
                appliedIntensity: budgetDecision.AppliedIntensity);
        }
    }
}

using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.
    DispatchCityRoadAccessMaintenance
{
    public sealed class DispatchCityRoadAccessMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard,
        CityMaintenanceBudgetAuthorizationService budgetAuthorizationService,
        TimeProvider timeProvider)
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
            CityBudgetAuthorizationDecision authorizationDecision =
                await budgetAuthorizationService.AuthorizeInfrastructureMaintenanceAsync(
                    cityId: request.CityId,
                    operationKind: "RoadAccessMaintenanceDispatch",
                    requestedIntensity: request.Intensity,
                    estimatedAmount: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                        systemName: "RoadAccess",
                        focus: request.Focus,
                        intensity: request.Intensity),
                    emergencyOverrideRequested: request.EmergencyOverride,
                    emergencyModeEnabled: state.RoadAccessInfrastructure.EmergencyModeEnabled,
                    defaultAuthorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                    defaultAvailableAmount: state.OperationalBudgetPressure.InfrastructureAvailableAmount,
                    pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    cancellationToken: cancellationToken);

            if (authorizationDecision.Denied)
            {
                decimal deniedSupport = pressureProfileFactory.Create(state)
                   .RoadSupport;

                return CityRoadAccessStatusDto.FromState(
                    cityId: request.CityId,
                    state: state,
                    roadSupportIndex: deniedSupport,
                    requestedIntensity: request.Intensity,
                    appliedIntensity: null,
                    budgetAuthorizationStatus: authorizationDecision.Status,
                    budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                    budgetAvailableAmount: authorizationDecision.AvailableAmount,
                    budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                    budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                    budgetAuthorizationSummary: authorizationDecision.Summary);
            }

            RoadAccessMaintenanceIntensity budgetAuthorizedIntensity = Enum.Parse<RoadAccessMaintenanceIntensity>(
                value: authorizationDecision.ApprovedIntensity ?? requestedIntensity.ToString(),
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: budgetAuthorizedIntensity.ToString(),
                authorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                emergencyModeEnabled: state.RoadAccessInfrastructure.EmergencyModeEnabled || request.EmergencyOverride);
            RoadAccessMaintenanceIntensity appliedIntensity = Enum.Parse<RoadAccessMaintenanceIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.ScheduleRoadAccessMaintenance(
                focus: focus,
                intensity: appliedIntensity,
                readyAtTickId: CalculateReadyAtTickId(
                    currentTickId: state.LastAppliedTickId,
                    intensity: budgetDecision.AppliedIntensity));
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "RoadAccess",
                    operationKind: "RoadAccessMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal roadSupport = pressureProfileFactory.Create(state)
               .RoadSupport;

            return CityRoadAccessStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                roadSupportIndex: roadSupport,
                requestedIntensity: request.Intensity,
                appliedIntensity: appliedIntensity.ToString(),
                budgetAuthorizationStatus: authorizationDecision.Status,
                budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                budgetAvailableAmount: authorizationDecision.AvailableAmount,
                budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                budgetAuthorizationSummary: authorizationDecision.Summary);
        }

        private static long CalculateReadyAtTickId(
            long currentTickId,
            string intensity)
        {
            long delay = string.Equals(
                a: intensity,
                b: "Heavy",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;

            return Math.Max(
                val1: 0,
                val2: currentTickId + delay);
        }
    }
}

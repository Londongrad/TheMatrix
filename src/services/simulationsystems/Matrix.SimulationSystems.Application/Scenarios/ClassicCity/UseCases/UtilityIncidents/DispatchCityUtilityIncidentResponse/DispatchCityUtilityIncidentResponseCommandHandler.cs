using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    DispatchCityUtilityIncidentResponse
{
    public sealed class DispatchCityUtilityIncidentResponseCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard,
        CityMaintenanceBudgetAuthorizationService budgetAuthorizationService,
        ICityOperationalTripDispatcher operationalTripDispatcher,
        TimeProvider timeProvider)
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
            CityBudgetAuthorizationDecision authorizationDecision =
                await budgetAuthorizationService.AuthorizeUtilityResponseAsync(
                    cityId: request.CityId,
                    operationKind: "UtilityIncidentResponseDispatch",
                    requestedIntensity: request.Intensity,
                    estimatedAmount: CityMaintenanceOperationalExpenseFactory.EstimateUtilityIncidentResponseAmount(
                        focus: request.Focus,
                        intensity: request.Intensity,
                        districtFocused: request.FocusDistrictId.HasValue),
                    emergencyOverrideRequested: request.EmergencyOverride,
                    emergencyModeEnabled: state.UtilityIncidentInfrastructure.EmergencyModeEnabled,
                    defaultAuthorizationLevel: state.OperationalBudgetPressure.OperationsAuthorizationLevel,
                    defaultAvailableAmount: state.OperationalBudgetPressure.OperationsAvailableAmount,
                    pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    cancellationToken: cancellationToken);

            if (authorizationDecision.Denied)
            {
                decimal deniedSupport = pressureProfileFactory.Create(state)
                   .UtilityIncidentSupport;

                return CityUtilityIncidentStatusDto.FromState(
                    cityId: request.CityId,
                    state: state,
                    utilityIncidentSupportIndex: deniedSupport,
                    requestedIntensity: request.Intensity,
                    appliedIntensity: null,
                    budgetAuthorizationStatus: authorizationDecision.Status,
                    budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                    budgetAvailableAmount: authorizationDecision.AvailableAmount,
                    budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                    budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                    budgetAuthorizationSummary: authorizationDecision.Summary);
            }

            UtilityIncidentResponseIntensity budgetAuthorizedIntensity = Enum.Parse<UtilityIncidentResponseIntensity>(
                value: authorizationDecision.ApprovedIntensity ?? requestedIntensity.ToString(),
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: budgetAuthorizedIntensity.ToString(),
                authorizationLevel: state.OperationalBudgetPressure.OperationsAuthorizationLevel,
                pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                emergencyModeEnabled: state.UtilityIncidentInfrastructure.EmergencyModeEnabled ||
                                      request.EmergencyOverride);
            UtilityIncidentResponseIntensity appliedIntensity = Enum.Parse<UtilityIncidentResponseIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.ScheduleUtilityIncidentResponse(
                focus: focus,
                intensity: appliedIntensity,
                focusDistrictId: request.FocusDistrictId,
                readyAtTickId: CalculateReadyAtTickId(
                    currentTickId: state.LastAppliedTickId,
                    intensity: budgetDecision.AppliedIntensity,
                    districtFocused: request.FocusDistrictId.HasValue));
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateUtilityIncidentResponseExpense(
                    cityId: request.CityId,
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    districtFocused: request.FocusDistrictId.HasValue,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.FocusDistrictId.HasValue)
                await operationalTripDispatcher.TryDispatchUtilityIncidentResponseAsync(
                    cityId: request.CityId,
                    focusDistrictId: request.FocusDistrictId.Value,
                    focus: request.Focus,
                    intensity: appliedIntensity.ToString(),
                    cancellationToken: cancellationToken);

            decimal utilityIncidentSupport = pressureProfileFactory.Create(state)
               .UtilityIncidentSupport;

            return CityUtilityIncidentStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                utilityIncidentSupportIndex: utilityIncidentSupport,
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
            string intensity,
            bool districtFocused)
        {
            long delay = string.Equals(
                a: intensity,
                b: "Heavy",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;

            if (districtFocused)
                delay = Math.Max(
                    val1: 1,
                    val2: delay - 1);

            return Math.Max(
                val1: 0,
                val2: currentTickId + delay);
        }
    }
}

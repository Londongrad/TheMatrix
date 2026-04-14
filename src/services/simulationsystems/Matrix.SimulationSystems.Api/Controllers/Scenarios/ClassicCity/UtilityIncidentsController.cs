using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.SetCityUtilityIncidentEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class UtilityIncidentsController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/utility-incidents")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityUtilityIncidentStatusDto? status = await mediator.Send(
                request: new GetCityUtilityIncidentStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpGet("{cityId:guid}/utility-incidents/districts")]
        public async Task<IResult> GetDistricts(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDistrictUtilityIncidentConditionsDto? status = await mediator.Send(
                request: new GetCityDistrictUtilityIncidentConditionsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToDistrictConditionsView(status));
        }

        [HttpPut("{cityId:guid}/utility-incidents/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCityUtilityIncidentEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CityUtilityIncidentStatusDto? status = await mediator.Send(
                request: new SetCityUtilityIncidentEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/utility-incidents/response-dispatch")]
        public async Task<IResult> DispatchResponse(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityUtilityIncidentResponseRequest request,
            CancellationToken cancellationToken)
        {
            CityUtilityIncidentStatusDto? status = await mediator.Send(
                request: new DispatchCityUtilityIncidentResponseCommand(
                    CityId: cityId,
                    Focus: request.Focus,
                    Intensity: request.Intensity,
                    EmergencyOverride: request.EmergencyOverride),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : string.Equals(
                    a: status.BudgetAuthorizationStatus,
                    b: "Denied",
                    comparisonType: StringComparison.OrdinalIgnoreCase)
                    ? Results.Conflict(MapToView(status))
                    : Results.Ok(MapToView(status));
        }

        private static CityUtilityIncidentStatusView MapToView(CityUtilityIncidentStatusDto dto)
        {
            return new CityUtilityIncidentStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                UtilityContinuityIndex: dto.UtilityContinuityIndex,
                UtilityIncidentSupportIndex: dto.UtilityIncidentSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                DispatchReadinessIndex: dto.DispatchReadinessIndex,
                RestorationCoverageIndex: dto.RestorationCoverageIndex,
                SpareCapacityIndex: dto.SpareCapacityIndex,
                FieldCoordinationIndex: dto.FieldCoordinationIndex,
                IncidentQueuePressureIndex: dto.IncidentQueuePressureIndex,
                RequestedIntensity: dto.RequestedIntensity,
                AppliedIntensity: dto.AppliedIntensity,
                BudgetAuthorizationStatus: dto.BudgetAuthorizationStatus,
                BudgetAuthorizationLevel: dto.BudgetAuthorizationLevel,
                BudgetAvailableAmount: dto.BudgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: dto.BudgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: dto.BudgetAuthorizedIntensity,
                BudgetAuthorizationSummary: dto.BudgetAuthorizationSummary,
                PendingOperation: MapPendingOperationView(dto.PendingOperation),
                System: new CityUtilityIncidentSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }

        private static PendingCityOperationView? MapPendingOperationView(PendingCityOperationDto? dto)
        {
            return dto is null
                ? null
                : new PendingCityOperationView(
                    Focus: dto.Focus,
                    Intensity: dto.Intensity,
                    ReadyAtTickId: dto.ReadyAtTickId);
        }

        private static CityDistrictUtilityIncidentConditionsView MapToDistrictConditionsView(
            CityDistrictUtilityIncidentConditionsDto dto)
        {
            return new CityDistrictUtilityIncidentConditionsView(
                CityId: dto.CityId,
                EffectiveTickId: dto.EffectiveTickId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                UtilityIncidentSupportIndex: dto.UtilityIncidentSupportIndex,
                Districts: dto.Districts
                   .Select(MapToDistrictConditionView)
                   .ToArray());
        }

        private static CityDistrictUtilityIncidentConditionView MapToDistrictConditionView(
            CityDistrictUtilityIncidentConditionDto dto)
        {
            return new CityDistrictUtilityIncidentConditionView(
                DistrictId: dto.DistrictId,
                UtilityContinuityIndex: dto.UtilityContinuityIndex,
                DispatchReadinessIndex: dto.DispatchReadinessIndex,
                IncidentPressureIndex: dto.IncidentPressureIndex,
                CoordinationDifficultyIndex: dto.CoordinationDifficultyIndex,
                RestorationPriorityIndex: dto.RestorationPriorityIndex);
        }
    }
}

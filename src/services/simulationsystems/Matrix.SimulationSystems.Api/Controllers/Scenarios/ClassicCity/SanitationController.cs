using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class SanitationController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/sanitation")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CitySanitationStatusDto? status = await mediator.Send(
                request: new GetCitySanitationStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpGet("{cityId:guid}/sanitation/districts")]
        public async Task<IResult> GetDistricts(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDistrictSanitationConditionsDto? status = await mediator.Send(
                request: new GetCityDistrictSanitationConditionsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToDistrictConditionsView(status));
        }

        [HttpPut("{cityId:guid}/sanitation/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCitySanitationEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CitySanitationStatusDto? status = await mediator.Send(
                request: new SetCitySanitationEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/sanitation/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCitySanitationMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CitySanitationStatusDto? status = await mediator.Send(
                request: new DispatchCitySanitationMaintenanceCommand(
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

        private static CitySanitationStatusView MapToView(CitySanitationStatusDto dto)
        {
            return new CitySanitationStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                SanitationCoverageIndex: dto.SanitationCoverageIndex,
                SanitationSupportIndex: dto.SanitationSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                TreatmentStabilityIndex: dto.TreatmentStabilityIndex,
                NetworkIntegrityIndex: dto.NetworkIntegrityIndex,
                OverflowControlIndex: dto.OverflowControlIndex,
                CrewReadinessIndex: dto.CrewReadinessIndex,
                IncidentPressureIndex: dto.IncidentPressureIndex,
                RequestedIntensity: dto.RequestedIntensity,
                AppliedIntensity: dto.AppliedIntensity,
                BudgetAuthorizationStatus: dto.BudgetAuthorizationStatus,
                BudgetAuthorizationLevel: dto.BudgetAuthorizationLevel,
                BudgetAvailableAmount: dto.BudgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: dto.BudgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: dto.BudgetAuthorizedIntensity,
                BudgetAuthorizationSummary: dto.BudgetAuthorizationSummary,
                PendingOperation: MapPendingOperationView(dto.PendingOperation),
                System: new CitySanitationSystemStatusView(
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

        private static CityDistrictSanitationConditionsView MapToDistrictConditionsView(
            CityDistrictSanitationConditionsDto dto)
        {
            return new CityDistrictSanitationConditionsView(
                CityId: dto.CityId,
                EffectiveTickId: dto.EffectiveTickId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                SanitationSupportIndex: dto.SanitationSupportIndex,
                Districts: dto.Districts
                    .Select(MapToDistrictConditionView)
                    .ToArray());
        }

        private static CityDistrictSanitationConditionView MapToDistrictConditionView(
            CityDistrictSanitationConditionDto dto)
        {
            return new CityDistrictSanitationConditionView(
                DistrictId: dto.DistrictId,
                SanitationCoverageIndex: dto.SanitationCoverageIndex,
                SanitationSupportIndex: dto.SanitationSupportIndex,
                OverflowRiskIndex: dto.OverflowRiskIndex,
                ContaminationRiskIndex: dto.ContaminationRiskIndex,
                MaintenancePriorityIndex: dto.MaintenancePriorityIndex);
        }
    }
}

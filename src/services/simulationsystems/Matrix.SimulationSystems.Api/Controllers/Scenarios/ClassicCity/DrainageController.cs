using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.GetCityDrainageStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.SetCityDrainageEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class DrainageController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/drainage")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDrainageStatusDto? status = await mediator.Send(
                request: new GetCityDrainageStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPut("{cityId:guid}/drainage/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCityDrainageEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CityDrainageStatusDto? status = await mediator.Send(
                request: new SetCityDrainageEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/drainage/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityDrainageMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CityDrainageStatusDto? status = await mediator.Send(
                request: new DispatchCityDrainageMaintenanceCommand(
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

        private static CityDrainageStatusView MapToView(CityDrainageStatusDto dto)
        {
            return new CityDrainageStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                FloodingIndex: dto.FloodingIndex,
                DrainageSupportIndex: dto.DrainageSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                PumpCapacityIndex: dto.PumpCapacityIndex,
                NetworkIntegrityIndex: dto.NetworkIntegrityIndex,
                BlockageIndex: dto.BlockageIndex,
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
                System: new CityDrainageSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }
    }
}

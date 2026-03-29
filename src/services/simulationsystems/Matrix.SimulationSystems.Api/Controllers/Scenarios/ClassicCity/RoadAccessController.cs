using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.SetCityRoadAccessEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class RoadAccessController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/road-access")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityRoadAccessStatusDto? status = await mediator.Send(
                request: new GetCityRoadAccessStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPut("{cityId:guid}/road-access/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCityRoadAccessEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CityRoadAccessStatusDto? status = await mediator.Send(
                request: new SetCityRoadAccessEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/road-access/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityRoadAccessMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CityRoadAccessStatusDto? status = await mediator.Send(
                request: new DispatchCityRoadAccessMaintenanceCommand(
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

        private static CityRoadAccessStatusView MapToView(CityRoadAccessStatusDto dto)
        {
            return new CityRoadAccessStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                FloodingIndex: dto.FloodingIndex,
                SnowAccumulationIndex: dto.SnowAccumulationIndex,
                RoadAccessibilityIndex: dto.RoadAccessibilityIndex,
                RoadSupportIndex: dto.RoadSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                CorridorAvailabilityIndex: dto.CorridorAvailabilityIndex,
                SurfaceIntegrityIndex: dto.SurfaceIntegrityIndex,
                TrafficControlReadinessIndex: dto.TrafficControlReadinessIndex,
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
                System: new CityRoadAccessSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }
    }
}

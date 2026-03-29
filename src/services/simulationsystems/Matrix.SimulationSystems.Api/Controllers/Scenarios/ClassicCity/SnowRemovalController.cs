using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.DispatchCitySnowRemovalMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.SetCitySnowRemovalEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class SnowRemovalController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/snow-removal")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CitySnowRemovalStatusDto? status = await mediator.Send(
                request: new GetCitySnowRemovalStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPut("{cityId:guid}/snow-removal/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCitySnowRemovalEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CitySnowRemovalStatusDto? status = await mediator.Send(
                request: new SetCitySnowRemovalEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/snow-removal/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCitySnowRemovalMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CitySnowRemovalStatusDto? status = await mediator.Send(
                request: new DispatchCitySnowRemovalMaintenanceCommand(
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

        private static CitySnowRemovalStatusView MapToView(CitySnowRemovalStatusDto dto)
        {
            return new CitySnowRemovalStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                SnowAccumulationIndex: dto.SnowAccumulationIndex,
                RoadAccessibilityIndex: dto.RoadAccessibilityIndex,
                SnowRemovalSupportIndex: dto.SnowRemovalSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                FleetAvailabilityIndex: dto.FleetAvailabilityIndex,
                RouteCoverageIndex: dto.RouteCoverageIndex,
                DeicingReadinessIndex: dto.DeicingReadinessIndex,
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
                System: new CitySnowRemovalSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }
    }
}

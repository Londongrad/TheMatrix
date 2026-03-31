using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.DispatchCityWaterDistributionMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityWaterDistributionStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.SetCityWaterDistributionEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class WaterDistributionController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/water-distribution")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityWaterDistributionStatusDto? status = await mediator.Send(
                request: new GetCityWaterDistributionStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPut("{cityId:guid}/water-distribution/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCityWaterDistributionEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CityWaterDistributionStatusDto? status = await mediator.Send(
                request: new SetCityWaterDistributionEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/water-distribution/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityWaterDistributionMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CityWaterDistributionStatusDto? status = await mediator.Send(
                request: new DispatchCityWaterDistributionMaintenanceCommand(
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

        private static CityWaterDistributionStatusView MapToView(CityWaterDistributionStatusDto dto)
        {
            return new CityWaterDistributionStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                WaterCoverageIndex: dto.WaterCoverageIndex,
                WaterSupportIndex: dto.WaterSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                TreatmentCapacityIndex: dto.TreatmentCapacityIndex,
                NetworkIntegrityIndex: dto.NetworkIntegrityIndex,
                PumpReadinessIndex: dto.PumpReadinessIndex,
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
                System: new CityWaterDistributionSystemStatusView(
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
    }
}

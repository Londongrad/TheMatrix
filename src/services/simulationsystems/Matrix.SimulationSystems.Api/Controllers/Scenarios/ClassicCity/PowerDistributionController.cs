using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.DispatchCityPowerDistributionMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityPowerDistributionStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.SetCityPowerDistributionEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class PowerDistributionController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/power-distribution")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityPowerDistributionStatusDto? status = await mediator.Send(
                request: new GetCityPowerDistributionStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpGet("{cityId:guid}/power-distribution/districts")]
        public async Task<IResult> GetDistricts(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDistrictPowerDistributionConditionsDto? status = await mediator.Send(
                request: new GetCityDistrictPowerDistributionConditionsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToDistrictConditionsView(status));
        }

        [HttpPut("{cityId:guid}/power-distribution/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCityPowerDistributionEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CityPowerDistributionStatusDto? status = await mediator.Send(
                request: new SetCityPowerDistributionEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/power-distribution/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityPowerDistributionMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CityPowerDistributionStatusDto? status = await mediator.Send(
                request: new DispatchCityPowerDistributionMaintenanceCommand(
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

        private static CityPowerDistributionStatusView MapToView(CityPowerDistributionStatusDto dto)
        {
            return new CityPowerDistributionStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                PowerCoverageIndex: dto.PowerCoverageIndex,
                PowerSupportIndex: dto.PowerSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                SubstationCapacityIndex: dto.SubstationCapacityIndex,
                GridIntegrityIndex: dto.GridIntegrityIndex,
                SwitchingReadinessIndex: dto.SwitchingReadinessIndex,
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
                System: new CityPowerDistributionSystemStatusView(
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

        private static CityDistrictPowerDistributionConditionsView MapToDistrictConditionsView(
            CityDistrictPowerDistributionConditionsDto dto)
        {
            return new CityDistrictPowerDistributionConditionsView(
                CityId: dto.CityId,
                EffectiveTickId: dto.EffectiveTickId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                PowerSupportIndex: dto.PowerSupportIndex,
                Districts: dto.Districts
                    .Select(MapToDistrictConditionView)
                    .ToArray());
        }

        private static CityDistrictPowerDistributionConditionView MapToDistrictConditionView(
            CityDistrictPowerDistributionConditionDto dto)
        {
            return new CityDistrictPowerDistributionConditionView(
                DistrictId: dto.DistrictId,
                PowerCoverageIndex: dto.PowerCoverageIndex,
                PowerSupportIndex: dto.PowerSupportIndex,
                OutageRiskIndex: dto.OutageRiskIndex,
                RestorationStrainIndex: dto.RestorationStrainIndex,
                MaintenancePriorityIndex: dto.MaintenancePriorityIndex);
        }
    }
}

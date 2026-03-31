using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.DispatchCityHeatingMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.SetCityHeatingEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class HeatingController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/heating")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityHeatingStatusDto? status = await mediator.Send(
                request: new GetCityHeatingStatusQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPut("{cityId:guid}/heating/emergency-mode")]
        public async Task<IResult> SetEmergencyMode(
            [FromRoute] Guid cityId,
            [FromBody] SetCityHeatingEmergencyModeRequest request,
            CancellationToken cancellationToken)
        {
            CityHeatingStatusDto? status = await mediator.Send(
                request: new SetCityHeatingEmergencyModeCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        [HttpPost("{cityId:guid}/heating/maintenance-dispatch")]
        public async Task<IResult> DispatchMaintenance(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityHeatingMaintenanceRequest request,
            CancellationToken cancellationToken)
        {
            CityHeatingStatusDto? status = await mediator.Send(
                request: new DispatchCityHeatingMaintenanceCommand(
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

        private static CityHeatingStatusView MapToView(CityHeatingStatusDto dto)
        {
            return new CityHeatingStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                HeatingCoverageIndex: dto.HeatingCoverageIndex,
                HeatingSupportIndex: dto.HeatingSupportIndex,
                BudgetPressureIndex: dto.BudgetPressureIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                PlantCapacityIndex: dto.PlantCapacityIndex,
                NetworkIntegrityIndex: dto.NetworkIntegrityIndex,
                ControlReadinessIndex: dto.ControlReadinessIndex,
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
                System: new CityHeatingSystemStatusView(
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

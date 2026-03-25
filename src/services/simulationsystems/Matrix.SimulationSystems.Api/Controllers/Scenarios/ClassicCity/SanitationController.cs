using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode;
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
                    Intensity: request.Intensity),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        private static CitySanitationStatusView MapToView(CitySanitationStatusDto dto)
        {
            return new CitySanitationStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                SanitationCoverageIndex: dto.SanitationCoverageIndex,
                SanitationSupportIndex: dto.SanitationSupportIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                TreatmentStabilityIndex: dto.TreatmentStabilityIndex,
                NetworkIntegrityIndex: dto.NetworkIntegrityIndex,
                OverflowControlIndex: dto.OverflowControlIndex,
                CrewReadinessIndex: dto.CrewReadinessIndex,
                IncidentPressureIndex: dto.IncidentPressureIndex,
                System: new CitySanitationSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }
    }
}

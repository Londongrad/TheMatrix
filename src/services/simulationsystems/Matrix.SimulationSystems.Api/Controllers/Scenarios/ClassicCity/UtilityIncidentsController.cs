using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.SetCityUtilityIncidentEmergencyMode;
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
                    Intensity: request.Intensity),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        private static CityUtilityIncidentStatusView MapToView(CityUtilityIncidentStatusDto dto)
        {
            return new CityUtilityIncidentStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                UtilityContinuityIndex: dto.UtilityContinuityIndex,
                UtilityIncidentSupportIndex: dto.UtilityIncidentSupportIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                DispatchReadinessIndex: dto.DispatchReadinessIndex,
                RestorationCoverageIndex: dto.RestorationCoverageIndex,
                SpareCapacityIndex: dto.SpareCapacityIndex,
                FieldCoordinationIndex: dto.FieldCoordinationIndex,
                IncidentQueuePressureIndex: dto.IncidentQueuePressureIndex,
                System: new CityUtilityIncidentSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }
    }
}

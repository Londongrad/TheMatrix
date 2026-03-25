using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.DispatchCityHeatingMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.SetCityHeatingEmergencyMode;
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
                    Intensity: request.Intensity),
                cancellationToken: cancellationToken);

            return status is null
                ? Results.NotFound()
                : Results.Ok(MapToView(status));
        }

        private static CityHeatingStatusView MapToView(CityHeatingStatusDto dto)
        {
            return new CityHeatingStatusView(
                CityId: dto.CityId,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                HeatingCoverageIndex: dto.HeatingCoverageIndex,
                HeatingSupportIndex: dto.HeatingSupportIndex,
                EmergencyModeEnabled: dto.EmergencyModeEnabled,
                PlantCapacityIndex: dto.PlantCapacityIndex,
                NetworkIntegrityIndex: dto.NetworkIntegrityIndex,
                ControlReadinessIndex: dto.ControlReadinessIndex,
                CrewReadinessIndex: dto.CrewReadinessIndex,
                IncidentPressureIndex: dto.IncidentPressureIndex,
                System: new CityHeatingSystemStatusView(
                    Kind: dto.System.Kind,
                    LoadIndex: dto.System.LoadIndex,
                    ServiceQualityIndex: dto.System.ServiceQualityIndex,
                    BacklogIndex: dto.System.BacklogIndex,
                    FailureRiskIndex: dto.System.FailureRiskIndex));
        }
    }
}

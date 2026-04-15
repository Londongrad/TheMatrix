using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.GetCityActiveTrips;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/cities/{cityId:guid}/trips")]
    public sealed class TripsController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IResult> Dispatch(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityTripRequest request,
            CancellationToken cancellationToken)
        {
            DispatchCityTripResult result = await mediator.Send(
                request: new DispatchCityTripCommand(
                    CityId: cityId,
                    FromKind: request.From.Kind,
                    FromId: request.From.Id,
                    ToKind: request.To.Kind,
                    ToId: request.To.Id,
                    Purpose: request.Purpose,
                    Profile: request.Profile,
                    MovementCapabilityIndex: request.MovementCapabilityIndex,
                    TravellerEntityId: request.TravellerEntityId,
                    Subject: request.Subject),
                cancellationToken: cancellationToken);

            return result.Status switch
            {
                DispatchCityTripStatus.Created => Results.Ok(MapToView(result.Trip!)),
                DispatchCityTripStatus.CityNotFound => Results.NotFound(),
                DispatchCityTripStatus.CityNotReady => Results.Conflict(
                    new
                    {
                        code = "SimulationCore.World.ActiveTrip.CityNotReady",
                        message = result.FailureReason
                    }),
                DispatchCityTripStatus.RouteUnavailable => Results.Conflict(
                    new
                    {
                        code = "SimulationCore.World.ActiveTrip.RouteUnavailable",
                        message = result.FailureReason
                    }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpGet("active")]
        public async Task<IResult> ListActive(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityActiveTripDto> trips = await mediator.Send(
                request: new GetCityActiveTripsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return Results.Ok(trips.Select(MapToView).ToArray());
        }

        private static CityActiveTripView MapToView(CityActiveTripDto dto)
        {
            return new CityActiveTripView(
                TripId: dto.TripId,
                CityId: dto.CityId,
                TravellerEntityId: dto.TravellerEntityId,
                Subject: dto.Subject,
                Purpose: dto.Purpose,
                Profile: dto.Profile,
                Status: dto.Status,
                MovementCapabilityIndex: dto.MovementCapabilityIndex,
                UsedDynamicRoadConditions: dto.UsedDynamicRoadConditions,
                PlannedAtTickId: dto.PlannedAtTickId,
                ConditionsEffectiveTickId: dto.ConditionsEffectiveTickId,
                LastAdvancedTickId: dto.LastAdvancedTickId,
                StartedAtSimTimeUtc: dto.StartedAtSimTimeUtc,
                LastAdvancedAtSimTimeUtc: dto.LastAdvancedAtSimTimeUtc,
                ExpectedArrivalAtSimTimeUtc: dto.ExpectedArrivalAtSimTimeUtc,
                ArrivedAtSimTimeUtc: dto.ArrivedAtSimTimeUtc,
                CurrentProgressIndex: dto.CurrentProgressIndex,
                TotalDistanceMeters: dto.TotalDistanceMeters,
                DistanceTravelledMeters: dto.DistanceTravelledMeters,
                RemainingDistanceMeters: dto.RemainingDistanceMeters,
                PlannedTravelTimeMinutes: dto.PlannedTravelTimeMinutes,
                AdjustedTravelTimeMinutes: dto.AdjustedTravelTimeMinutes,
                From: new CityActiveTripEndpointView(
                    Kind: dto.From.Kind,
                    EntityId: dto.From.EntityId,
                    DistrictId: dto.From.DistrictId,
                    RoadNodeId: dto.From.RoadNodeId,
                    Name: dto.From.Name,
                    PositionX: dto.From.PositionX,
                    PositionY: dto.From.PositionY),
                To: new CityActiveTripEndpointView(
                    Kind: dto.To.Kind,
                    EntityId: dto.To.EntityId,
                    DistrictId: dto.To.DistrictId,
                    RoadNodeId: dto.To.RoadNodeId,
                    Name: dto.To.Name,
                    PositionX: dto.To.PositionX,
                    PositionY: dto.To.PositionY),
                Current: new CityActiveTripProgressView(
                    DistrictId: dto.Current.DistrictId,
                    RoadSegmentId: dto.Current.RoadSegmentId,
                    SegmentProgressIndex: dto.Current.SegmentProgressIndex,
                    PositionX: dto.Current.PositionX,
                    PositionY: dto.Current.PositionY));
        }
    }
}

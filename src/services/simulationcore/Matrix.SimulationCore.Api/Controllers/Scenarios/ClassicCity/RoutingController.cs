using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoutesBatch;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route(ClassicCityApiRoutes.RoutingRoute)]
    public sealed class RoutingController(IMediator mediator) : ControllerBase
    {
        [HttpPost("resolve")]
        public async Task<IResult> Resolve(
            [FromRoute] Guid cityId,
            [FromBody] ResolveCityRouteRequest request,
            CancellationToken cancellationToken)
        {
            CityRouteDto? route = await mediator.Send(
                request: new ResolveCityRouteQuery(
                    CityId: cityId,
                    FromKind: request.From.Kind,
                    FromId: request.From.Id,
                    ToKind: request.To.Kind,
                    ToId: request.To.Id,
                    Profile: request.Profile),
                cancellationToken: cancellationToken);

            return route is null
                ? Results.NotFound()
                : Results.Ok(MapToView(route));
        }

        [HttpPost("resolve-batch")]
        public async Task<IResult> ResolveBatch(
            [FromRoute] Guid cityId,
            [FromBody] ResolveCityRoutesBatchRequest request,
            CancellationToken cancellationToken)
        {
            ResolveCityRoutesBatchResult result = await mediator.Send(
                request: new ResolveCityRoutesBatchQuery(
                    CityId: cityId,
                    Routes: (request.Routes ?? [])
                   .Select((
                        route,
                        index) => new ResolveCityRoutesBatchQueryItem(
                        Index: index,
                        FromKind: route.From.Kind,
                        FromId: route.From.Id,
                        ToKind: route.To.Kind,
                        ToId: route.To.Id,
                        Profile: route.Profile))
                   .ToArray()),
                cancellationToken: cancellationToken);

            return Results.Ok(
                new ResolveCityRoutesBatchView(
                    Routes: result.Routes
                       .Select(x => new ResolvedCityRouteBatchItemView(
                            Index: x.Index,
                            Found: x.Route is not null,
                            Route: x.Route is null
                                ? null
                                : MapToView(x.Route)))
                       .ToArray()));
        }

        private static CityRouteView MapToView(CityRouteDto dto)
        {
            return new CityRouteView(
                CityId: dto.CityId,
                Profile: dto.Profile,
                Accessible: dto.Accessible,
                UsedDynamicRoadConditions: dto.UsedDynamicRoadConditions,
                EffectiveTickId: dto.EffectiveTickId,
                ConditionsLastEvaluatedAtUtc: dto.ConditionsLastEvaluatedAtUtc,
                From: MapToPointView(dto.From),
                To: MapToPointView(dto.To),
                TotalDistanceMeters: dto.TotalDistanceMeters,
                EstimatedTravelTimeMinutes: dto.EstimatedTravelTimeMinutes,
                OverallPassabilityIndex: dto.OverallPassabilityIndex,
                UnreachableReason: dto.UnreachableReason,
                Segments: dto.Segments
                   .Select(MapToSegmentView)
                   .ToArray());
        }

        private static CityRoutePointView MapToPointView(CityRoutePointDto dto)
        {
            return new CityRoutePointView(
                Kind: dto.Kind,
                EntityId: dto.EntityId,
                DistrictId: dto.DistrictId,
                RoadNodeId: dto.RoadNodeId,
                Name: dto.Name,
                PositionX: dto.PositionX,
                PositionY: dto.PositionY);
        }

        private static CityRouteSegmentView MapToSegmentView(CityRouteSegmentDto dto)
        {
            return new CityRouteSegmentView(
                RoadSegmentId: dto.RoadSegmentId,
                DistrictId: dto.DistrictId,
                FromRoadNodeId: dto.FromRoadNodeId,
                ToRoadNodeId: dto.ToRoadNodeId,
                Name: dto.Name,
                Type: dto.Type,
                LengthMeters: dto.LengthMeters,
                EstimatedTraversalMinutes: dto.EstimatedTraversalMinutes,
                PassabilityIndex: dto.PassabilityIndex,
                SpeedMultiplierIndex: dto.SpeedMultiplierIndex,
                SlipRiskIndex: dto.SlipRiskIndex,
                ClosureRiskIndex: dto.ClosureRiskIndex);
        }
    }
}

using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed record GetCityRoadSegmentConditionsQuery(Guid CityId)
        : IRequest<CityRoadSegmentConditionsDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}

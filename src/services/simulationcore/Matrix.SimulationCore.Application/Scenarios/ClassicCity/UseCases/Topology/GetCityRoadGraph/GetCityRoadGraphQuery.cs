using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph
{
    public sealed record GetCityRoadGraphQuery(Guid CityId)
        : IRequest<CityRoadGraphDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityRead;
    }
}

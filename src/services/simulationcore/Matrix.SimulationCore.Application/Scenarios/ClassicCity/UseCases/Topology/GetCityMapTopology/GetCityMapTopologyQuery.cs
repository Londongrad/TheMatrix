using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed record GetCityMapTopologyQuery(Guid CityId)
        : IRequest<CityMapTopologyDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityRead;
    }
}

using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public sealed record ResolveCityRouteQuery(
        Guid CityId,
        string FromKind,
        Guid FromId,
        string ToKind,
        Guid ToId,
        string Profile) : IRequest<CityRouteDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityRead;
    }
}

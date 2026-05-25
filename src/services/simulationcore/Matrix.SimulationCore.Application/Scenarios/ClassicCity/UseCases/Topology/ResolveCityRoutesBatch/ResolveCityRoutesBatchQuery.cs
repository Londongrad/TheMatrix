using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoutesBatch
{
    public sealed record ResolveCityRoutesBatchQuery(
        Guid CityId,
        IReadOnlyList<ResolveCityRoutesBatchQueryItem> Routes)
        : IRequest<ResolveCityRoutesBatchResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityRead;
    }

    public sealed record ResolveCityRoutesBatchQueryItem(
        int Index,
        string FromKind,
        Guid FromId,
        string ToKind,
        Guid ToId,
        string Profile);

    public sealed record ResolveCityRoutesBatchResult(IReadOnlyList<ResolvedCityRouteBatchItemDto> Routes);

    public sealed record ResolvedCityRouteBatchItemDto(
        int Index,
        CityRouteDto? Route);
}

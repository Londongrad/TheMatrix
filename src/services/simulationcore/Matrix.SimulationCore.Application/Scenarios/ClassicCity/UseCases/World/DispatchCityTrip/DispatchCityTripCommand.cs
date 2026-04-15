using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.SimulationCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip
{
    public sealed record DispatchCityTripCommand(
        Guid CityId,
        string FromKind,
        Guid FromId,
        string ToKind,
        Guid ToId,
        string Purpose,
        string Profile,
        decimal MovementCapabilityIndex,
        Guid? TravellerEntityId,
        string? Subject) : IRequest<DispatchCityTripResult>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.SimulationCoreClassicCityRead,
            AppPermissionKeys.SimulationCoreClassicCityUpdate
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}

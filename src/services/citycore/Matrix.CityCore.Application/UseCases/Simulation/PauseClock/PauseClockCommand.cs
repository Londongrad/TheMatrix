using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.CityCore.Application.UseCases.Simulation.PauseClock
{
    public sealed record PauseClockCommand(Guid SimulationId) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreSimulationRead,
            AppPermissionKeys.CityCoreSimulationControl
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}

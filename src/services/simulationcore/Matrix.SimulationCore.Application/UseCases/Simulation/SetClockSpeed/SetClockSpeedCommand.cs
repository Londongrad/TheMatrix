using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed
{
    public sealed record SetClockSpeedCommand(
        Guid SimulationId,
        decimal Multiplier) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreSimulationRead,
            AppPermissionKeys.CityCoreSimulationControl
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}

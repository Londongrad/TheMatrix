using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock
{
    public sealed record ResumeClockCommand(Guid SimulationId) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreSimulationRead,
            AppPermissionKeys.CityCoreSimulationControl
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}

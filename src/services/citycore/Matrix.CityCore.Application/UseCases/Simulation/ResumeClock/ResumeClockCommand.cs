using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.ResumeClock
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

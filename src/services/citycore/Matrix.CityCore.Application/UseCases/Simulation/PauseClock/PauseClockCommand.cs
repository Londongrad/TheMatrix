using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.PauseClock
{
    public sealed record PauseClockCommand(Guid SimulationId) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreSimulationControl;
    }
}

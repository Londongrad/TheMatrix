using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
{
    public sealed record GetClockQuery(Guid SimulationId) : IRequest<ClockDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreSimulationRead;
    }
}

using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    SetCityPowerDistributionEmergencyMode
{
    public sealed record SetCityPowerDistributionEmergencyModeCommand(
        Guid CityId,
        bool Enabled)
        : IRequest<CityPowerDistributionStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}

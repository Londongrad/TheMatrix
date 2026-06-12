using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    SetCityWaterDistributionEmergencyMode
{
    public sealed record SetCityWaterDistributionEmergencyModeCommand(
        Guid CityId,
        bool Enabled)
        : IRequest<CityWaterDistributionStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}

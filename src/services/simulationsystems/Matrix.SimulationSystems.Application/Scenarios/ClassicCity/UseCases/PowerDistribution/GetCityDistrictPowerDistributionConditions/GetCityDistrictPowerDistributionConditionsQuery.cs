using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityDistrictPowerDistributionConditions
{
    public sealed record GetCityDistrictPowerDistributionConditionsQuery(Guid CityId)
        : IRequest<CityDistrictPowerDistributionConditionsDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}

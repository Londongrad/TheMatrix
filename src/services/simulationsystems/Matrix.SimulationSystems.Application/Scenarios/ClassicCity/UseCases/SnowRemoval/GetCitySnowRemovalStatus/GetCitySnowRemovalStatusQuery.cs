using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus
{
    public sealed record GetCitySnowRemovalStatusQuery(Guid CityId)
        : IRequest<CitySnowRemovalStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}

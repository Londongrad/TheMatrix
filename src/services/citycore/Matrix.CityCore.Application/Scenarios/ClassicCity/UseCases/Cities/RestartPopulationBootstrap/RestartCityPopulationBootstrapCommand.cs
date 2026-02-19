using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed record RestartCityPopulationBootstrapCommand(Guid CityId)
        : IRequest<RestartCityPopulationBootstrapResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityPopulationBootstrapRetry;
    }
}

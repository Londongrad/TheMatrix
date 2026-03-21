using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather
{
    public sealed record GetWeatherQuery(Guid CityId) : IRequest<CityWeatherDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityRead;
    }
}

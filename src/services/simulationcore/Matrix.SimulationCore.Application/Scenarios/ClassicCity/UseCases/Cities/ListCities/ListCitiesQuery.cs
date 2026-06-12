using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities
{
    public sealed record ListCitiesQuery(bool IncludeArchived)
        : IRequest<IReadOnlyList<CityDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityRead;
    }
}

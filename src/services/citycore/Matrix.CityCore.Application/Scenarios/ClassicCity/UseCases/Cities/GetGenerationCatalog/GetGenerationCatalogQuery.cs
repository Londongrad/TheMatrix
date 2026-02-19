using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog
{
    public sealed record GetGenerationCatalogQuery : IRequest<CityGenerationCatalogDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityCreate;
    }
}

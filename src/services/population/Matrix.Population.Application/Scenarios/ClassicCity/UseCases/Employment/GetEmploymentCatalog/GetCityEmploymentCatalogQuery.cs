using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog
{
    public sealed record GetCityEmploymentCatalogQuery(Guid CityId)
        : IRequest<CityEmploymentCatalogDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEmploymentManage;
    }
}

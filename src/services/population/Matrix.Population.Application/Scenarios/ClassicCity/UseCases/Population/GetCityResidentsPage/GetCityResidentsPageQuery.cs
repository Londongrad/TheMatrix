using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed record GetCityResidentsPageQuery(
        Guid CityId,
        DateOnly CurrentDate,
        Pagination Pagination) : IRequest<PagedResult<CityResidentSummaryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleRead;
    }
}

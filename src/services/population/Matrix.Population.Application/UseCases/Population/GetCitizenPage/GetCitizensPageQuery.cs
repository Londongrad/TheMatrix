using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.GetCitizenPage
{
    public sealed record GetCitizensPageQuery(Pagination Pagination)
        : IRequest<PagedResult<PersonDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleRead;
    }
}

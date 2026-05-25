using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinesses
{
    public sealed record GetCityBusinessesQuery(Guid CityId)
        : IRequest<IReadOnlyList<CityBusinessDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesRead;
    }
}

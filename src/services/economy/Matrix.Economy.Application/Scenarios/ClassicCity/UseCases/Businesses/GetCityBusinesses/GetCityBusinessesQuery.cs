using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinesses
{
    public sealed record GetCityBusinessesQuery(Guid CityId)
        : IRequest<IReadOnlyList<CityBusinessDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesRead;
    }
}

using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames
{
    public sealed record GetSuggestedCityNamesQuery(
        string? Seed,
        int Count = 12) : IRequest<SuggestedCityNamesDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityCreate;
    }
}

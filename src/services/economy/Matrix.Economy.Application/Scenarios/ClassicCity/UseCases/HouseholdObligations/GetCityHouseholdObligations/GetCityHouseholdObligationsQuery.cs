using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetCityHouseholdObligations
{
    public sealed record GetCityHouseholdObligationsQuery(Guid CityId)
        : IRequest<IReadOnlyList<CityHouseholdObligationDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdObligationsRead;
    }
}

using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetHouseholdObligations
{
    public sealed record GetHouseholdObligationsQuery(Guid HouseholdAccountId)
        : IRequest<IReadOnlyList<CityHouseholdObligationDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdObligationsRead;
    }
}

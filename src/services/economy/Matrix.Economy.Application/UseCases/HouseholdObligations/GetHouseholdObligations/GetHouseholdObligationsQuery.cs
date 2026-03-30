using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations
{
    public sealed record GetHouseholdObligationsQuery(Guid HouseholdAccountId)
        : IRequest<IReadOnlyList<CityHouseholdObligationDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdObligationsRead;
    }
}

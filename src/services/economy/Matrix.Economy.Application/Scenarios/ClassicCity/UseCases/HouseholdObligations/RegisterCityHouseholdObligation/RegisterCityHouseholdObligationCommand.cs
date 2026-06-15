using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RegisterCityHouseholdObligation
{
    public sealed record RegisterCityHouseholdObligationCommand(
        Guid CityId,
        Guid HouseholdAccountId,
        Guid ProviderBusinessId,
        string Name,
        CityHouseholdObligationKind Kind,
        CityHouseholdObligationBillingCadence BillingCadence,
        decimal ChargeAmount,
        decimal TaxAmount,
        DateTimeOffset? FirstChargeDueAtUtc) : IRequest<CityHouseholdObligationDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdObligationsManage;
    }
}

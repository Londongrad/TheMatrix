using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RegisterCityHouseholdObligation
{
    public sealed record RegisterCityHouseholdObligationCommand(
        Guid CityId,
        Guid HouseholdAccountId,
        Guid ProviderBusinessId,
        string Name,
        CityHouseholdObligationKind Kind,
        decimal ChargeAmount,
        decimal TaxAmount) : IRequest<CityHouseholdObligationDto>;
}

using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount
{
    public sealed record RegisterCityHouseholdAccountCommand(
        Guid CityId,
        string Name,
        string? ExternalReferenceCode,
        decimal OpeningBalance,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<CityHouseholdAccountDto>;
}

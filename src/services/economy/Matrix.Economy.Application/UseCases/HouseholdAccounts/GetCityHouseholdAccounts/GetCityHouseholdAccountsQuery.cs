using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccounts
{
    public sealed record GetCityHouseholdAccountsQuery(Guid CityId) : IRequest<IReadOnlyList<CityHouseholdAccountDto>>;
}

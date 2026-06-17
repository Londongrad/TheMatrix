using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccounts
{
    public sealed class GetCityHouseholdAccountsQueryHandler(ICityHouseholdAccountRepository householdAccountRepository)
        : IRequestHandler<GetCityHouseholdAccountsQuery, IReadOnlyList<CityHouseholdAccountDto>>
    {
        public async Task<IReadOnlyList<CityHouseholdAccountDto>> Handle(
            GetCityHouseholdAccountsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdAccount> accounts = await householdAccountRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            return accounts.Select(Map)
               .ToArray();
        }

        private static CityHouseholdAccountDto Map(CityHouseholdAccount account)
        {
            return new CityHouseholdAccountDto(
                HouseholdAccountId: account.Id,
                CityId: account.CityId,
                CreatedAtUtc: account.CreatedAtUtc.ToString("O"),
                Name: account.Name,
                ExternalReferenceCode: account.ExternalReferenceCode,
                UnitKind: account.UnitKind.ToString(),
                UnitCode: account.UnitCode,
                UnitDisplayName: account.UnitDisplayName,
                UnitSymbol: account.UnitSymbol,
                Balance: account.Balance.Amount,
                TotalOpeningBalance: account.TotalOpeningBalance.Amount,
                TotalPayrollIncome: account.TotalPayrollIncome.Amount,
                TotalConsumerSpending: account.TotalConsumerSpending.Amount);
        }
    }
}

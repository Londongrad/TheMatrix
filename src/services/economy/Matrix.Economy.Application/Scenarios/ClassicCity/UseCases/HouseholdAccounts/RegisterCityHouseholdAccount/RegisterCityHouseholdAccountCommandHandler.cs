using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount
{
    public sealed class RegisterCityHouseholdAccountCommandHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<RegisterCityHouseholdAccountCommand, CityHouseholdAccountDto>
    {
        public async Task<CityHouseholdAccountDto> Handle(
            RegisterCityHouseholdAccountCommand request,
            CancellationToken cancellationToken)
        {
            CityBudgetUnitProfile unitProfile = ResolveRequestedUnit(request);
            DateTimeOffset createdAtUtc = timeProvider.GetUtcNow();

            var account = new CityHouseholdAccount(
                id: Guid.NewGuid(),
                cityId: request.CityId,
                name: request.Name,
                externalReferenceCode: request.ExternalReferenceCode,
                createdAtUtc: createdAtUtc,
                unitProfile: unitProfile,
                openingBalance: Money.FromDecimal(request.OpeningBalance));

            householdAccountRepository.Add(account);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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

        private static CityBudgetUnitProfile ResolveRequestedUnit(RegisterCityHouseholdAccountCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.UnitCode) &&
                string.IsNullOrWhiteSpace(request.UnitDisplayName) &&
                string.IsNullOrWhiteSpace(request.UnitSymbol) &&
                string.IsNullOrWhiteSpace(request.UnitKind))
                return CityBudgetUnitProfile.DefaultMoney();

            if (!Enum.TryParse(
                    value: request.UnitKind,
                    ignoreCase: true,
                    result: out CityBudgetUnitKind unitKind))
                throw new InvalidOperationException($"Unsupported unit kind '{request.UnitKind}'.");

            return new CityBudgetUnitProfile(
                Kind: unitKind,
                Code: request.UnitCode ?? string.Empty,
                DisplayName: request.UnitDisplayName ?? string.Empty,
                Symbol: request.UnitSymbol ?? string.Empty);
        }
    }
}

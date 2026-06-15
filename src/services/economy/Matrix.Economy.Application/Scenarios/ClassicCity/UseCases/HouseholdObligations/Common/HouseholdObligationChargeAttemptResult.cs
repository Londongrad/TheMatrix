using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common
{
    public sealed record HouseholdObligationChargeAttemptResult(
        bool Succeeded,
        string? FailureCode,
        CityHouseholdAccountLedgerEntryDto? LedgerEntry,
        Money ChargedAmount,
        Money ChargedTaxAmount,
        int SettledInstallmentCount)
    {
        public static HouseholdObligationChargeAttemptResult Success(
            CityHouseholdAccountLedgerEntryDto ledgerEntry,
            Money chargedAmount,
            Money chargedTaxAmount,
            int settledInstallmentCount)
        {
            return new HouseholdObligationChargeAttemptResult(
                Succeeded: true,
                FailureCode: null,
                LedgerEntry: ledgerEntry,
                ChargedAmount: chargedAmount,
                ChargedTaxAmount: chargedTaxAmount,
                SettledInstallmentCount: settledInstallmentCount);
        }

        public static HouseholdObligationChargeAttemptResult Failure(string failureCode)
        {
            return new HouseholdObligationChargeAttemptResult(
                Succeeded: false,
                FailureCode: failureCode,
                LedgerEntry: null,
                ChargedAmount: Money.Zero,
                ChargedTaxAmount: Money.Zero,
                SettledInstallmentCount: 0);
        }
    }
}

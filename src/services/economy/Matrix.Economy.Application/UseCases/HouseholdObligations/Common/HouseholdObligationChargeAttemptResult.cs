using Matrix.Economy.Application.UseCases.HouseholdAccounts;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.Common
{
    public sealed record HouseholdObligationChargeAttemptResult(
        bool Succeeded,
        string? FailureCode,
        CityHouseholdAccountLedgerEntryDto? LedgerEntry)
    {
        public static HouseholdObligationChargeAttemptResult Success(CityHouseholdAccountLedgerEntryDto ledgerEntry)
        {
            return new HouseholdObligationChargeAttemptResult(
                Succeeded: true,
                FailureCode: null,
                LedgerEntry: ledgerEntry);
        }

        public static HouseholdObligationChargeAttemptResult Failure(string failureCode)
        {
            return new HouseholdObligationChargeAttemptResult(
                Succeeded: false,
                FailureCode: failureCode,
                LedgerEntry: null);
        }
    }
}

using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge
{
    public sealed record IssueHouseholdObligationChargeCommand(
        Guid ObligationId,
        string? Description) : IRequest<CityHouseholdAccountLedgerEntryDto>;
}

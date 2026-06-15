using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge
{
    public sealed class IssueHouseholdObligationChargeCommandHandler(
        ICityHouseholdObligationRepository obligationRepository,
        HouseholdObligationChargeSupport chargeSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<IssueHouseholdObligationChargeCommand, CityHouseholdAccountLedgerEntryDto>
    {
        public async Task<CityHouseholdAccountLedgerEntryDto> Handle(
            IssueHouseholdObligationChargeCommand request,
            CancellationToken cancellationToken)
        {
            CityHouseholdObligation obligation =
                await obligationRepository.GetByIdAsync(
                    obligationId: request.ObligationId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException($"Obligation '{request.ObligationId}' was not found.");
            CityHouseholdAccountLedgerEntryDto result = await chargeSupport.ChargeAsync(
                obligation: obligation,
                description: request.Description,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

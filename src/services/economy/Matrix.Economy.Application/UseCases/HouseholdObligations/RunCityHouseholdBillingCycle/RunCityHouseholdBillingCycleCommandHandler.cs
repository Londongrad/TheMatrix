using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed class RunCityHouseholdBillingCycleCommandHandler(
        ICityHouseholdObligationRepository obligationRepository,
        HouseholdObligationChargeSupport chargeSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RunCityHouseholdBillingCycleCommand, RunCityHouseholdBillingCycleResultDto>
    {
        public async Task<RunCityHouseholdBillingCycleResultDto> Handle(
            RunCityHouseholdBillingCycleCommand request,
            CancellationToken cancellationToken)
        {
            DateTimeOffset asOfUtc = request.AsOfUtc ?? DateTimeOffset.UtcNow;

            IReadOnlyList<CityHouseholdObligation> dueObligations = await obligationRepository.ListDueByCityAsync(
                request.CityId,
                asOfUtc,
                cancellationToken);

            decimal totalChargedAmount = 0m;
            decimal totalTaxAmount = 0m;

            foreach (CityHouseholdObligation obligation in dueObligations)
            {
                await chargeSupport.ChargeAsync(obligation, "Recurring billing cycle.", cancellationToken);
                totalChargedAmount += obligation.ChargeAmount.Amount;
                totalTaxAmount += obligation.TaxAmount.Amount;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RunCityHouseholdBillingCycleResultDto(
                CityId: request.CityId,
                AsOfUtc: asOfUtc.ToString("O"),
                ChargedObligations: dueObligations.Count,
                TotalChargedAmount: totalChargedAmount,
                TotalTaxAmount: totalTaxAmount);
        }
    }
}

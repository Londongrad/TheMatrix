using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Simulation.Common;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed class RunCityHouseholdBillingCycleCommandHandler(
        ICityPopulationSignalPublisher cityPopulationSignalPublisher,
        CityEconomyRecurringCycleExecutionService recurringCycleExecutionService,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RunCityHouseholdBillingCycleCommand, RunCityHouseholdBillingCycleResultDto>
    {
        public async Task<RunCityHouseholdBillingCycleResultDto> Handle(
            RunCityHouseholdBillingCycleCommand request,
            CancellationToken cancellationToken)
        {
            DateTimeOffset asOfUtc = request.AsOfUtc ?? DateTimeOffset.UtcNow;

            CityEconomyBillingCycleExecutionResult executionResult =
                await recurringCycleExecutionService.ExecuteBillingAsync(
                    cityId: request.CityId,
                    asOfUtc: asOfUtc,
                    cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var batch in executionResult.FinancialStressBatches)
                await cityPopulationSignalPublisher.PublishClassicCityHouseholdFinancialStressBatchAsync(
                    batch: batch,
                    cancellationToken: cancellationToken);

            return executionResult.Result;
        }
    }
}

using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed class RunCityHouseholdBillingCycleCommandHandler(
        ICityPopulationSignalPublisher cityPopulationSignalPublisher,
        CityEconomyRecurringCycleExecutionService recurringCycleExecutionService,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<RunCityHouseholdBillingCycleCommand, RunCityHouseholdBillingCycleResultDto>
    {
        public async Task<RunCityHouseholdBillingCycleResultDto> Handle(
            RunCityHouseholdBillingCycleCommand request,
            CancellationToken cancellationToken)
        {
            DateTimeOffset asOfUtc = request.AsOfUtc ?? timeProvider.GetUtcNow();
            RunCityHouseholdBillingCycleResultDto result = default!;

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                CityEconomyBillingCycleExecutionResult executionResult =
                    await recurringCycleExecutionService.ExecuteBillingAsync(
                        cityId: request.CityId,
                        asOfUtc: asOfUtc,
                        cancellationToken: ct);

                await unitOfWork.SaveChangesAsync(ct);

                foreach (var batch in executionResult.FinancialStressBatches)
                    await cityPopulationSignalPublisher.PublishClassicCityHouseholdFinancialStressBatchAsync(
                        batch: batch,
                        cancellationToken: ct);

                await unitOfWork.SaveChangesAsync(ct);
                result = executionResult.Result;
            }, cancellationToken);

            return result;
        }
    }
}

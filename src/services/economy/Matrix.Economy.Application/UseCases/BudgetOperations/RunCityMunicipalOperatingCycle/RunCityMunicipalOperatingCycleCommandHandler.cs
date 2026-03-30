using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed class RunCityMunicipalOperatingCycleCommandHandler(
        CityEconomyRecurringCycleExecutionService recurringCycleExecutionService,
        IEconomyUnitOfWork unitOfWork,
        ICityOperationalBudgetSignalPublisher operationalBudgetSignalPublisher,
        ICityOperationalBudgetPressureProjectionService pressureProjectionService)
        : IRequestHandler<RunCityMunicipalOperatingCycleCommand, RunCityMunicipalOperatingCycleResultDto>
    {
        public async Task<RunCityMunicipalOperatingCycleResultDto> Handle(
            RunCityMunicipalOperatingCycleCommand request,
            CancellationToken cancellationToken)
        {
            RunCityMunicipalOperatingCycleResultDto result = default!;

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                result = await recurringCycleExecutionService.ExecuteMunicipalOperatingCycleAsync(
                    cityId: request.CityId,
                    cancellationToken: ct);

                await unitOfWork.SaveChangesAsync(ct);

                CityOperationalBudgetPressureDto pressure = await pressureProjectionService.GetAsync(
                    cityId: request.CityId,
                    cancellationToken: ct);
                DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;
                await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                    snapshot: pressure,
                    effectiveAtUtc: occurredAtUtc,
                    occurredAtUtc: occurredAtUtc,
                    cancellationToken: ct);
                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return result;
        }
    }
}

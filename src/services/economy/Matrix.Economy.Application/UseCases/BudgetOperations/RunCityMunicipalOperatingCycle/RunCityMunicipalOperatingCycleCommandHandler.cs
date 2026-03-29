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
        ISender sender)
        : IRequestHandler<RunCityMunicipalOperatingCycleCommand, RunCityMunicipalOperatingCycleResultDto>
    {
        public async Task<RunCityMunicipalOperatingCycleResultDto> Handle(
            RunCityMunicipalOperatingCycleCommand request,
            CancellationToken cancellationToken)
        {
            RunCityMunicipalOperatingCycleResultDto result =
                await recurringCycleExecutionService.ExecuteMunicipalOperatingCycleAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            CityOperationalBudgetPressureDto pressure = await sender.Send(
                request: new GetCityOperationalBudgetPressureQuery(request.CityId),
                cancellationToken: cancellationToken);
            await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                snapshot: pressure,
                effectiveAtUtc: DateTimeOffset.UtcNow,
                occurredAtUtc: DateTimeOffset.UtcNow,
                cancellationToken: cancellationToken);

            return result;
        }
    }
}

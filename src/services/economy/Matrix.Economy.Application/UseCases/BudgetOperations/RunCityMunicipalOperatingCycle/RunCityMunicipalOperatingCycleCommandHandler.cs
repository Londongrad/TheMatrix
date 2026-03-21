using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed class RunCityMunicipalOperatingCycleCommandHandler(
        CityEconomyRecurringCycleExecutionService recurringCycleExecutionService,
        IEconomyUnitOfWork unitOfWork)
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

            return result;
        }
    }
}

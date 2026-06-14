using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RunCityBusinessTaxCycle
{
    public sealed class RunCityBusinessTaxCycleCommandHandler(
        CityEconomyRecurringCycleExecutionService recurringCycleExecutionService,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RunCityBusinessTaxCycleCommand, RunCityBusinessTaxCycleResultDto>
    {
        public async Task<RunCityBusinessTaxCycleResultDto> Handle(
            RunCityBusinessTaxCycleCommand request,
            CancellationToken cancellationToken)
        {
            RunCityBusinessTaxCycleResultDto result =
                await recurringCycleExecutionService.ExecuteTaxCycleAsync(
                    cityId: request.CityId,
                    budgetCategory: request.BudgetCategory,
                    cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return result;
        }
    }
}

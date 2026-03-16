using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle
{
    public sealed class RunCityBusinessTaxCycleCommandHandler(
        ICityBusinessRepository businessRepository,
        CityBusinessTaxRemittanceSupport taxRemittanceSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RunCityBusinessTaxCycleCommand, RunCityBusinessTaxCycleResultDto>
    {
        public async Task<RunCityBusinessTaxCycleResultDto> Handle(
            RunCityBusinessTaxCycleCommand request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            int remittedBusinesses = 0;
            decimal totalRemittedAmount = 0m;

            foreach (CityBusiness business in businesses.Where(x => x.TaxReserve.IsPositive))
            {
                decimal remittanceAmount = business.TaxReserve.Amount;

                await taxRemittanceSupport.RemitAsync(
                    business: business,
                    amount: business.TaxReserve,
                    budgetCategory: request.BudgetCategory,
                    title: $"{business.Name} scheduled tax remittance",
                    description: "Recurring city business tax cycle.",
                    cancellationToken: cancellationToken);

                remittedBusinesses++;
                totalRemittedAmount += remittanceAmount;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RunCityBusinessTaxCycleResultDto(
                CityId: request.CityId,
                BudgetCategory: request.BudgetCategory.ToString(),
                RemittedBusinesses: remittedBusinesses,
                TotalRemittedAmount: totalRemittedAmount);
        }
    }
}

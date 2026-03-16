using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RemitCityBusinessTax
{
    public sealed class RemitCityBusinessTaxCommandHandler(
        ICityBusinessRepository businessRepository,
        CityBusinessTaxRemittanceSupport taxRemittanceSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RemitCityBusinessTaxCommand, CityBusinessLedgerEntryDto>
    {
        public async Task<CityBusinessLedgerEntryDto> Handle(
            RemitCityBusinessTaxCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new InvalidOperationException(
                                        $"Business '{request.BusinessId}' was not found.");

            var amount = Money.FromDecimal(request.Amount);
            CityBusinessLedgerEntryDto result = await taxRemittanceSupport.RemitAsync(
                business: business,
                amount: amount,
                budgetCategory: request.BudgetCategory,
                title: request.Title,
                description: request.Description,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

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
            CityBusiness business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.BusinessId}' was not found.");

            Money amount = Money.FromDecimal(request.Amount);
            CityBusinessLedgerEntryDto result = await taxRemittanceSupport.RemitAsync(
                business,
                amount,
                request.BudgetCategory,
                request.Title,
                request.Description,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

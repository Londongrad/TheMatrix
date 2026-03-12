using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed class DisburseCityBudgetToBusinessCommandHandler(
        ICityBusinessRepository businessRepository,
        CityBudgetBusinessDisbursementSupport disbursementSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<DisburseCityBudgetToBusinessCommand, BudgetLedgerEntryDto>
    {
        public async Task<BudgetLedgerEntryDto> Handle(
            DisburseCityBudgetToBusinessCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.BusinessId}' was not found.");

            if (business.CityId != request.CityId)
            {
                throw new InvalidOperationException("Business and budget must belong to the same city.");
            }

            BudgetLedgerEntryDto result = await disbursementSupport.DisburseAsync(
                business,
                request.Category,
                request.Amount,
                request.Title,
                request.Description,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

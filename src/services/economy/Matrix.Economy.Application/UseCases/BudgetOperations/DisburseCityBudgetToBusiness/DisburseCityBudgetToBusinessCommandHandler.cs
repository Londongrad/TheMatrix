using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed class DisburseCityBudgetToBusinessCommandHandler(
        ICityBusinessRepository businessRepository,
        CityBudgetBusinessDisbursementSupport disbursementSupport,
        IEconomyUnitOfWork unitOfWork,
        ICityOperationalBudgetSignalPublisher operationalBudgetSignalPublisher,
        ISender sender)
        : IRequestHandler<DisburseCityBudgetToBusinessCommand, BudgetLedgerEntryDto>
    {
        public async Task<BudgetLedgerEntryDto> Handle(
            DisburseCityBudgetToBusinessCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new InvalidOperationException(
                                        $"Business '{request.BusinessId}' was not found.");

            if (business.CityId != request.CityId)
                throw new InvalidOperationException("Business and budget must belong to the same city.");

            BudgetLedgerEntryDto result = await disbursementSupport.DisburseAsync(
                business: business,
                category: request.Category,
                amount: request.Amount,
                title: request.Title,
                description: request.Description,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            CityOperationalBudgetPressureDto pressure = await sender.Send(
                request: new GetCityOperationalBudgetPressureQuery(request.CityId),
                cancellationToken: cancellationToken);
            await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                snapshot: pressure,
                effectiveAtUtc: DateTimeOffset.Parse(result.OccurredAtUtc),
                occurredAtUtc: DateTimeOffset.UtcNow,
                cancellationToken: cancellationToken);
            return result;
        }
    }
}

using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed class DisburseCityBudgetToBusinessCommandHandler(
        ICityBusinessRepository businessRepository,
        CityBudgetBusinessDisbursementSupport disbursementSupport,
        IEconomyUnitOfWork unitOfWork,
        ICityOperationalBudgetSignalPublisher operationalBudgetSignalPublisher,
        ICityOperationalBudgetPressureProjectionService pressureProjectionService,
        TimeProvider timeProvider)
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

            BudgetLedgerEntryDto result = default!;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    result = await disbursementSupport.DisburseAsync(
                        business: business,
                        category: request.Category,
                        amount: request.Amount,
                        title: request.Title,
                        description: request.Description,
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    CityOperationalBudgetPressureDto pressure = await pressureProjectionService.GetAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);
                    await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                        snapshot: pressure,
                        effectiveAtUtc: DateTimeOffset.Parse(result.OccurredAtUtc),
                        occurredAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return result;
        }
    }
}

using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityWorkplaceBusinessSyncConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBusinessRepository businessRepository,
        ICityEconomyDeletionRepository deletionRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityWorkplaceBusinessSyncConsumer> logger)
        : IConsumer<ClassicCityWorkplaceBusinessSyncBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityWorkplaceBusinessSyncBatchV1> context)
        {
            ClassicCityWorkplaceBusinessSyncBatchV1 message = context.Message;

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.CityId,
                    cancellationToken: context.CancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped workplace business sync for deleted cityId={CityId}, correlationId={CorrelationId}.",
                    message.CityId,
                    message.CorrelationId);
                return;
            }

            CityBudget budget = await CityBudgetInitializationSupport.EnsureExistsAsync(
                cityId: message.CityId,
                budgetRepository: budgetRepository,
                unitOfWork: unitOfWork,
                cancellationToken: context.CancellationToken);

            var existingBusinessesByExternalReference = (await businessRepository.ListByCityAsync(
                    cityId: message.CityId,
                    cancellationToken: context.CancellationToken))
               .Where(x => !string.IsNullOrWhiteSpace(x.ExternalReferenceCode))
               .ToDictionary(
                    keySelector: x => x.ExternalReferenceCode!,
                    elementSelector: x => x,
                    comparer: StringComparer.Ordinal);

            int createdBusinesses = 0;

            foreach (ClassicCityWorkplaceBusinessSyncItemV1 workplace in message.Workplaces)
            {
                if (existingBusinessesByExternalReference.TryGetValue(
                        key: workplace.ExternalReferenceCode,
                        value: out CityBusiness? existingBusiness))
                {
                    existingBusiness.EnsureCompatibleUnit(budget.GetUnitProfile());
                    existingBusiness.EnsureCanIssuePayroll();
                    continue;
                }

                var business = new CityBusiness(
                    id: Guid.NewGuid(),
                    cityId: message.CityId,
                    name: workplace.Name,
                    externalReferenceCode: workplace.ExternalReferenceCode,
                    templateKey: null,
                    kind: CityBusinessKind.Employer,
                    createdAtUtc: message.OccurredAtUtc,
                    unitProfile: budget.GetUnitProfile(),
                    initialCapital: Money.Zero);

                businessRepository.Add(business);
                existingBusinessesByExternalReference[workplace.ExternalReferenceCode] = business;
                createdBusinesses++;
            }

            if (createdBusinesses == 0)
            {
                logger.LogDebug(
                    message:
                    "Skipped classic city workplace business sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}; all workplace businesses already exist.",
                    message.CityId,
                    message.CorrelationId,
                    message.BatchNumber,
                    message.TotalBatches);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                message:
                "Applied classic city workplace business sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, createdBusinesses={CreatedBusinesses}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                createdBusinesses);
        }
    }
}

using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class ClassicCityWorkplaceBusinessSyncConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBusinessRepository businessRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityWorkplaceBusinessSyncConsumer> logger)
        : IConsumer<ClassicCityWorkplaceBusinessSyncBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityWorkplaceBusinessSyncBatchV1> context)
        {
            ClassicCityWorkplaceBusinessSyncBatchV1 message = context.Message;

            CityBudget? existingBudget = await budgetRepository.GetByCityAsync(message.CityId, context.CancellationToken);
            bool budgetCreated = existingBudget is null;
            CityBudget budget = existingBudget ?? CreateBudget(message.CityId, budgetRepository);

            Dictionary<string, CityBusiness> existingBusinessesByExternalReference = (await businessRepository.ListByCityAsync(
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
                if (existingBusinessesByExternalReference.TryGetValue(workplace.ExternalReferenceCode, out CityBusiness? existingBusiness))
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

            if (!budgetCreated && createdBusinesses == 0)
            {
                logger.LogDebug(
                    "Skipped classic city workplace business sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}; all workplace businesses already exist.",
                    message.CityId,
                    message.CorrelationId,
                    message.BatchNumber,
                    message.TotalBatches);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "Applied classic city workplace business sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, budgetCreated={BudgetCreated}, createdBusinesses={CreatedBusinesses}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                budgetCreated,
                createdBusinesses);
        }

        private static CityBudget CreateBudget(Guid cityId, ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), cityId);
            budgetRepository.Add(budget);
            return budget;
        }
    }
}

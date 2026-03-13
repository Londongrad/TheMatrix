using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class ClassicCityHouseholdAccountSyncConsumer(
        ICityBudgetRepository budgetRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityHouseholdAccountSyncConsumer> logger)
        : IConsumer<ClassicCityHouseholdAccountSyncBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityHouseholdAccountSyncBatchV1> context)
        {
            ClassicCityHouseholdAccountSyncBatchV1 message = context.Message;

            CityBudget? existingBudget = await budgetRepository.GetByCityAsync(message.CityId, context.CancellationToken);
            bool budgetCreated = existingBudget is null;
            CityBudget budget = existingBudget ?? CreateBudget(message.CityId, budgetRepository);
            int createdAccounts = 0;

            foreach (ClassicCityHouseholdAccountSyncItemV1 household in message.Households)
            {
                CityHouseholdAccount? existing = await householdAccountRepository.GetByCityAndExternalReferenceCodeAsync(
                    cityId: message.CityId,
                    externalReferenceCode: household.ExternalReferenceCode,
                    cancellationToken: context.CancellationToken);

                if (existing is not null)
                {
                    existing.EnsureCompatibleUnit(budget.GetUnitProfile());
                    continue;
                }

                householdAccountRepository.Add(
                    new CityHouseholdAccount(
                        id: Guid.NewGuid(),
                        cityId: message.CityId,
                        name: household.Name,
                        externalReferenceCode: household.ExternalReferenceCode,
                        createdAtUtc: household.CreatedAtUtc,
                        unitProfile: budget.GetUnitProfile(),
                        openingBalance: Money.FromDecimal(household.OpeningBalanceAmount)));
                createdAccounts++;
            }

            if (!budgetCreated && createdAccounts == 0)
            {
                logger.LogDebug(
                    "Skipped classic city household account sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}; all accounts already exist.",
                    message.CityId,
                    message.CorrelationId,
                    message.BatchNumber,
                    message.TotalBatches);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "Applied classic city household account sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, budgetCreated={BudgetCreated}, createdAccounts={CreatedAccounts}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                budgetCreated,
                createdAccounts);
        }

        private static CityBudget CreateBudget(Guid cityId, ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), cityId);
            budgetRepository.Add(budget);
            return budget;
        }
    }
}

using MassTransit;
using Matrix.CityCore.Contracts.Events;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityCreatedConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBudgetAllocationRepository allocationRepository,
        IEconomyUnitOfWork unitOfWork,
        CityEconomySimulationTemplatePolicy simulationTemplatePolicy,
        ILogger<CityCreatedConsumer> logger)
        : IConsumer<CityCreatedV1>
    {
        public async Task Consume(ConsumeContext<CityCreatedV1> context)
        {
            CityCreatedV1 message = context.Message;
            var template = simulationTemplatePolicy.Resolve(message.SimulationKind);

            CityBudget? budget = await budgetRepository.GetByCityAsync(message.CityId, context.CancellationToken);
            bool budgetCreated = false;
            int createdAllocations = 0;

            if (budget is null)
            {
                budget = new CityBudget(CityBudgetId.New(), message.CityId, template.UnitProfile);
                budgetRepository.Add(budget);
                budgetCreated = true;
            }
            else
            {
                budget.EnsureCompatibleUnit(template.UnitProfile);
            }

            foreach (CityEconomyAllocationTemplate allocationTemplate in template.DefaultAllocations)
            {
                CityBudgetAllocation? existing = await allocationRepository.GetByCityAndCategoryAsync(
                    cityId: message.CityId,
                    category: allocationTemplate.Category,
                    cancellationToken: context.CancellationToken);

                if (existing is not null)
                {
                    existing.EnsureCompatibleUnit(template.UnitProfile);
                    continue;
                }

                allocationRepository.Add(
                    new CityBudgetAllocation(
                        id: Guid.NewGuid(),
                        cityId: message.CityId,
                        category: allocationTemplate.Category,
                        createdAtUtc: message.CreatedAtUtc,
                        unitProfile: template.UnitProfile,
                        targetAmount: allocationTemplate.TargetAmount));

                createdAllocations++;
            }

            if (!budgetCreated && createdAllocations == 0)
            {
                logger.LogDebug(
                    "Skipped city economy initialization for cityId={CityId}; budget and default allocations already exist.",
                    message.CityId);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "Initialized economy context for cityId={CityId}, simulationKind={SimulationKind}, budgetCreated={BudgetCreated}, createdAllocations={CreatedAllocations}.",
                message.CityId,
                message.SimulationKind,
                budgetCreated,
                createdAllocations);
        }
    }
}

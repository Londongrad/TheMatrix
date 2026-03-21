using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityCreatedConsumer(
        ICityEconomyBootstrapService cityEconomyBootstrapService,
        ILogger<CityCreatedConsumer> logger)
        : IConsumer<CityCreatedV1>
    {
        public async Task Consume(ConsumeContext<CityCreatedV1> context)
        {
            CityCreatedV1 message = context.Message;
            CityEconomyBootstrapResultDto result = await cityEconomyBootstrapService.BootstrapAsync(
                cityId: message.CityId,
                simulationKind: message.SimulationKind,
                economyProfile: message.EconomyProfile,
                createdAtUtc: message.CreatedAtUtc,
                cancellationToken: context.CancellationToken);

            if (!result.BudgetCreated && result.CreatedAllocations == 0 && result.CreatedBusinesses == 0)
            {
                logger.LogDebug(
                    message:
                    "Skipped city economy initialization for cityId={CityId}; budget, default allocations, and template businesses already exist.",
                    message.CityId);
                return;
            }

            logger.LogInformation(
                message:
                "Initialized economy context for cityId={CityId}, simulationKind={SimulationKind}, economyProfile={EconomyProfile}, budgetCreated={BudgetCreated}, createdAllocations={CreatedAllocations}, createdBusinesses={CreatedBusinesses}.",
                message.CityId,
                message.SimulationKind,
                message.EconomyProfile,
                result.BudgetCreated,
                result.CreatedAllocations,
                result.CreatedBusinesses);
        }
    }
}

using MassTransit;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumer(
        ICityEconomyBootstrapService cityEconomyBootstrapService,
        ICityEconomyDeletionRepository deletionRepository,
        ILogger<CityCreatedConsumer> logger)
        : IConsumer<ClassicCityCreatedV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityCreatedV1> context)
        {
            await ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityCreatedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey))
            {
                logger.LogDebug(
                    message:
                    "Ignored classic-city-created event for simulationId={SimulationId}, scenarioKey={ScenarioKey}, hostTypeKey={HostTypeKey}.",
                    message.SimulationId,
                    message.ScenarioKey,
                    message.HostTypeKey);
                return;
            }

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.HostId,
                    cancellationToken: cancellationToken) is not null)
            {
                logger.LogWarning(
                    message: "Skipped city economy initialization for deleted cityId={CityId}.",
                    message.HostId);
                return;
            }

            CityEconomyBootstrapResultDto result = await cityEconomyBootstrapService.BootstrapAsync(
                cityId: message.HostId,
                scenarioKey: message.ScenarioKey,
                economyProfile: message.EconomyProfile,
                createdAtUtc: message.CreatedAtUtc,
                cancellationToken: cancellationToken);

            if (!result.BudgetCreated && result.CreatedAllocations == 0 && result.CreatedBusinesses == 0)
            {
                logger.LogDebug(
                    message:
                    "Skipped city economy initialization for cityId={CityId}; budget, default allocations, and template businesses already exist.",
                    message.HostId);
                return;
            }

            logger.LogInformation(
                message:
                "Initialized economy context for cityId={CityId}, scenarioKey={ScenarioKey}, economyProfile={EconomyProfile}, budgetCreated={BudgetCreated}, createdAllocations={CreatedAllocations}, createdBusinesses={CreatedBusinesses}.",
                message.HostId,
                message.ScenarioKey,
                message.EconomyProfile,
                result.BudgetCreated,
                result.CreatedAllocations,
                result.CreatedBusinesses);
        }
    }
}

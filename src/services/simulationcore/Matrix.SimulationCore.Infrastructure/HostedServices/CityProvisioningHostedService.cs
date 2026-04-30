using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.SimulationCore.Infrastructure.HostedServices
{
    public sealed class CityProvisioningHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ProvisioningRecoveryOptions> options,
        TimeProvider timeProvider,
        ILogger<CityProvisioningHostedService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ProvisioningRecoveryOptions recoveryOptions = options.Value;

            if (recoveryOptions.PollIntervalSeconds <= 0)
                throw new InvalidOperationException("SimulationCore:Provisioning:PollIntervalSeconds must be > 0.");

            if (recoveryOptions.LeaseDurationSeconds <= 0)
                throw new InvalidOperationException("SimulationCore:Provisioning:LeaseDurationSeconds must be > 0.");

            if (recoveryOptions.MaxBatchSize <= 0)
                throw new InvalidOperationException("SimulationCore:Provisioning:MaxBatchSize must be > 0.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(recoveryOptions.PollIntervalSeconds));

            try
            {
                await ProcessBatchAsync(
                    recoveryOptions: recoveryOptions,
                    cancellationToken: cancellationToken);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        await ProcessBatchAsync(
                            recoveryOptions: recoveryOptions,
                            cancellationToken: cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            exception: ex,
                            message: "SimulationCore provisioning recovery loop iteration failed.");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("SimulationCore provisioning recovery loop stopped.");
            }
        }

        private async Task ProcessBatchAsync(
            ProvisioningRecoveryOptions recoveryOptions,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Guid> candidates = await ListCandidatesAsync(
                recoveryOptions: recoveryOptions,
                cancellationToken: cancellationToken);

            foreach (Guid cityId in candidates)
            {
                await ProcessCandidateAsync(
                    cityId: cityId,
                    recoveryOptions: recoveryOptions,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task<IReadOnlyList<Guid>> ListCandidatesAsync(
            ProvisioningRecoveryOptions recoveryOptions,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ICityRepository cityRepository = scope.ServiceProvider.GetRequiredService<ICityRepository>();

            IReadOnlyList<City> cities = await cityRepository.ListRecoverableProvisioningAsync(
                asOfUtc: timeProvider.GetUtcNow(),
                limit: recoveryOptions.MaxBatchSize,
                cancellationToken: cancellationToken);

            return cities
               .Select(x => x.Id.Value)
               .ToArray();
        }

        private async Task ProcessCandidateAsync(
            Guid cityId,
            ProvisioningRecoveryOptions recoveryOptions,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ICityRepository cityRepository = scope.ServiceProvider.GetRequiredService<ICityRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IClassicCityProvisioningOrchestrator orchestrator =
                scope.ServiceProvider.GetRequiredService<IClassicCityProvisioningOrchestrator>();

            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(cityId),
                cancellationToken: cancellationToken);

            if (city is null || !city.IsProvisioning)
                return;

            TimeSpan leaseDuration = TimeSpan.FromSeconds(recoveryOptions.LeaseDurationSeconds);
            DateTimeOffset nowUtc = timeProvider.GetUtcNow();

            if (!city.TryAcquireProvisioningLease(
                    acquiredAtUtc: nowUtc,
                    leaseDuration: leaseDuration))
                return;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            async Task HeartbeatAsync(CancellationToken heartbeatCancellationToken)
            {
                if (!city.TryRefreshProvisioningLease(
                        heartbeatAtUtc: timeProvider.GetUtcNow(),
                        leaseDuration: leaseDuration))
                    return;

                await unitOfWork.SaveChangesAsync(heartbeatCancellationToken);
            }

            try
            {
                await orchestrator.ProvisionAsync(
                    cityId: city.Id.Value,
                    simulationKind: city.SimulationKind.ToString(),
                    populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                    economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                    plannedPeopleCountOverride: city.GenerationProfile.PlannedPeopleCount,
                    heartbeatAsync: HeartbeatAsync,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "SimulationCore provisioning worker failed while processing cityId={CityId}.",
                    cityId);
            }
        }
    }
}

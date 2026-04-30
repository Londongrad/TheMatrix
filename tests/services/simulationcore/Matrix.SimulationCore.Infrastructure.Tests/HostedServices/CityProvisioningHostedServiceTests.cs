using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Infrastructure.HostedServices;
using Matrix.SimulationCore.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.HostedServices;

public sealed class CityProvisioningHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_UsesInjectedTimeProviderForRecoveryAndHeartbeat()
    {
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);
        DateTimeOffset recoveryAtUtc = createdAtUtc.AddMinutes(15);
        DateTimeOffset heartbeatAtUtc = recoveryAtUtc.AddMinutes(2);
        var timeProvider = new HostedServicesTestSupport.MutableTimeProvider(recoveryAtUtc);
        City city = HostedServicesTestSupport.CreateProvisioningCity(createdAtUtc);
        var cityRepository = new HostedServicesTestSupport.FakeCityRepository
        {
            RecoverableCities = [city]
        };
        cityRepository.CitiesById[city.Id.Value] = city;
        var unitOfWork = new HostedServicesTestSupport.FakeUnitOfWork();
        var orchestrator = new HostedServicesTestSupport.FakeClassicCityProvisioningOrchestrator
        {
            OnProvisionAsync = async (heartbeatAsync, cancellationToken) =>
            {
                Assert.NotNull(heartbeatAsync);

                timeProvider.SetUtcNow(heartbeatAtUtc);
                await heartbeatAsync!(cancellationToken);
            }
        };
        using var service = CreateService(
            cityRepository,
            unitOfWork,
            orchestrator,
            timeProvider,
            new ProvisioningRecoveryOptions
            {
                PollIntervalSeconds = 60,
                LeaseDurationSeconds = 180,
                MaxBatchSize = 8
            });

        await service.StartAsync(CancellationToken.None);
        await orchestrator.ProvisionCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(recoveryAtUtc, cityRepository.RequestedRecoverableAsOfUtc);
        Assert.Equal(8, cityRepository.RequestedRecoverableLimit);
        Assert.Equal(city.Id, Assert.Single(cityRepository.RequestedCityIds));
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(city.Id.Value, orchestrator.RequestedCityId);
        Assert.Equal(city.SimulationKind.ToString(), orchestrator.RequestedSimulationKind);
        Assert.Equal(city.GenerationProfile.PlannedPeopleCount, orchestrator.RequestedPlannedPeopleCountOverride);
        Assert.Equal(heartbeatAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(heartbeatAtUtc.AddSeconds(180), city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(1, city.ProvisioningAttemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProvisioningLeaseIsStillActive_SkipsOrchestration()
    {
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);
        DateTimeOffset leasedAtUtc = createdAtUtc.AddMinutes(10);
        DateTimeOffset recoveryAtUtc = leasedAtUtc.AddSeconds(30);
        var timeProvider = new HostedServicesTestSupport.MutableTimeProvider(recoveryAtUtc);
        City city = HostedServicesTestSupport.CreateProvisioningCity(createdAtUtc);
        bool leaseAcquired = city.TryAcquireProvisioningLease(
            acquiredAtUtc: leasedAtUtc,
            leaseDuration: TimeSpan.FromMinutes(5));
        Assert.True(leaseAcquired);

        var cityRepository = new HostedServicesTestSupport.FakeCityRepository
        {
            RecoverableCities = [city]
        };
        cityRepository.CitiesById[city.Id.Value] = city;
        var unitOfWork = new HostedServicesTestSupport.FakeUnitOfWork();
        var orchestrator = new HostedServicesTestSupport.FakeClassicCityProvisioningOrchestrator();
        using var service = CreateService(
            cityRepository,
            unitOfWork,
            orchestrator,
            timeProvider,
            new ProvisioningRecoveryOptions
            {
                PollIntervalSeconds = 60,
                LeaseDurationSeconds = 180,
                MaxBatchSize = 4
            });

        await service.StartAsync(CancellationToken.None);
        await cityRepository.GetByIdCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, orchestrator.ProvisionCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Equal(leasedAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(leasedAtUtc.AddMinutes(5), city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(1, city.ProvisioningAttemptCount);
    }

    private static CityProvisioningHostedService CreateService(
        HostedServicesTestSupport.FakeCityRepository cityRepository,
        HostedServicesTestSupport.FakeUnitOfWork unitOfWork,
        HostedServicesTestSupport.FakeClassicCityProvisioningOrchestrator orchestrator,
        TimeProvider timeProvider,
        ProvisioningRecoveryOptions options)
    {
        var services = new Dictionary<Type, object>
        {
            [typeof(ICityRepository)] = cityRepository,
            [typeof(IUnitOfWork)] = unitOfWork,
            [typeof(IClassicCityProvisioningOrchestrator)] = orchestrator
        };

        return new CityProvisioningHostedService(
            new HostedServicesTestSupport.TestServiceScopeFactory(
                new HostedServicesTestSupport.DictionaryServiceProvider(services)),
            Microsoft.Extensions.Options.Options.Create(options),
            timeProvider,
            NullLogger<CityProvisioningHostedService>.Instance);
    }
}

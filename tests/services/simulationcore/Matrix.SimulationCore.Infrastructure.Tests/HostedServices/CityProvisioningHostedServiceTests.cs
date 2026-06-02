using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Infrastructure.HostedServices;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Provisioning;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.HostedServices
{
    public sealed class CityProvisioningHostedServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_UsesInjectedTimeProviderForRecoveryAndHeartbeat()
        {
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
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
                OnProvisionAsync = async (
                    heartbeatAsync,
                    cancellationToken) =>
                {
                    Assert.NotNull(heartbeatAsync);

                    timeProvider.SetUtcNow(heartbeatAtUtc);
                    await heartbeatAsync!(cancellationToken);
                }
            };
            using CityProvisioningHostedService service = CreateService(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork,
                orchestrator: orchestrator,
                timeProvider: timeProvider,
                options: new ProvisioningRecoveryOptions
                {
                    PollIntervalSeconds = 60,
                    LeaseDurationSeconds = 180,
                    MaxBatchSize = 8
                });

            await service.StartAsync(CancellationToken.None);
            await orchestrator.ProvisionCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopAsync(CancellationToken.None);

            Assert.Equal(
                expected: recoveryAtUtc,
                actual: cityRepository.RequestedRecoverableAsOfUtc);
            Assert.Equal(
                expected: 8,
                actual: cityRepository.RequestedRecoverableLimit);
            Assert.Equal(
                expected: city.Id,
                actual: Assert.Single(cityRepository.RequestedCityIds));
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: city.Id.Value,
                actual: orchestrator.RequestedCityId);
            Assert.Equal(
                expected: city.GenerationProfile.PlannedPeopleCount,
                actual: orchestrator.RequestedPlannedPeopleCountOverride);
            Assert.Equal(
                expected: heartbeatAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: heartbeatAtUtc.AddSeconds(180),
                actual: city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 1,
                actual: city.ProvisioningAttemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProvisioningLeaseIsStillActive_SkipsOrchestration()
        {
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
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
            using CityProvisioningHostedService service = CreateService(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork,
                orchestrator: orchestrator,
                timeProvider: timeProvider,
                options: new ProvisioningRecoveryOptions
                {
                    PollIntervalSeconds = 60,
                    LeaseDurationSeconds = 180,
                    MaxBatchSize = 4
                });

            await service.StartAsync(CancellationToken.None);
            await cityRepository.GetByIdCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopAsync(CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: orchestrator.ProvisionCallCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: leasedAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: leasedAtUtc.AddMinutes(5),
                actual: city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 1,
                actual: city.ProvisioningAttemptCount);
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
                scopeFactory: new HostedServicesTestSupport.TestServiceScopeFactory(
                    new HostedServicesTestSupport.DictionaryServiceProvider(services)),
                options: Microsoft.Extensions.Options.Options.Create(options),
                timeProvider: timeProvider,
                logger: NullLogger<CityProvisioningHostedService>.Instance);
        }
    }
}

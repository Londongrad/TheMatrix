using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityLeaseAndArchiveTests
    {
        [Fact]
        public void TryAcquireProvisioningLease_WhenProvisioningAndLeaseIsFree_AcquiresLease()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var leaseDuration = TimeSpan.FromMinutes(10);

            city.ClearDomainEvents();

            bool result = city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                leaseDuration: leaseDuration);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.Provisioning,
                actual: city.Status);
            Assert.Equal(
                expected: CityTestData.CreatedAtUtc,
                actual: city.ProvisioningStartedAtUtc);
            Assert.Equal(
                expected: CityTestData.LeaseAcquiredAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: CityTestData.LeaseAcquiredAtUtc.Add(leaseDuration),
                actual: city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 1,
                actual: city.ProvisioningAttemptCount);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryAcquireProvisioningLease_WhenCityIsNotProvisioning_ReturnsFalse()
        {
            City city = CityTestData.CreateCity();

            city.ClearDomainEvents();

            bool result = city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                leaseDuration: TimeSpan.FromMinutes(10));

            Assert.False(result);
            Assert.Equal(
                expected: CityStatus.Active,
                actual: city.Status);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 0,
                actual: city.ProvisioningAttemptCount);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryAcquireProvisioningLease_WhenLeaseIsStillActive_ReturnsFalseWithoutIncrementingAttempts()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var leaseDuration = TimeSpan.FromMinutes(10);

            city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                leaseDuration: leaseDuration);
            city.ClearDomainEvents();

            bool result = city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc.AddMinutes(5),
                leaseDuration: leaseDuration);

            Assert.False(result);
            Assert.Equal(
                expected: CityTestData.LeaseAcquiredAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: CityTestData.LeaseAcquiredAtUtc.Add(leaseDuration),
                actual: city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 1,
                actual: city.ProvisioningAttemptCount);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryAcquireProvisioningLease_WithNonPositiveDuration_ThrowsInvalidOperationException()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => city.TryAcquireProvisioningLease(
                    acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                    leaseDuration: TimeSpan.Zero));

            Assert.Equal(
                expected: "Provisioning lease duration must be greater than zero.",
                actual: exception.Message);
        }

        [Fact]
        public void TryRefreshProvisioningLease_WhenLeaseIsActive_RefreshesHeartbeatAndExpiration()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var leaseDuration = TimeSpan.FromMinutes(10);

            city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                leaseDuration: leaseDuration);
            city.ClearDomainEvents();

            bool result = city.TryRefreshProvisioningLease(
                heartbeatAtUtc: CityTestData.LeaseHeartbeatAtUtc,
                leaseDuration: leaseDuration);

            Assert.True(result);
            Assert.Equal(
                expected: CityTestData.LeaseHeartbeatAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: CityTestData.LeaseHeartbeatAtUtc.Add(leaseDuration),
                actual: city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 1,
                actual: city.ProvisioningAttemptCount);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryRefreshProvisioningLease_WhenLeaseHasExpired_ReturnsFalse()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var leaseDuration = TimeSpan.FromMinutes(10);

            city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                leaseDuration: leaseDuration);
            city.ClearDomainEvents();

            bool result = city.TryRefreshProvisioningLease(
                heartbeatAtUtc: CityTestData.LeaseAcquiredAtUtc.AddMinutes(11),
                leaseDuration: leaseDuration);

            Assert.False(result);
            Assert.Equal(
                expected: CityTestData.LeaseAcquiredAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: CityTestData.LeaseAcquiredAtUtc.Add(leaseDuration),
                actual: city.ProvisioningLeaseExpiresAtUtc);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryRefreshProvisioningLease_WithNonPositiveDuration_ThrowsInvalidOperationException()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => city.TryRefreshProvisioningLease(
                    heartbeatAtUtc: CityTestData.LeaseHeartbeatAtUtc,
                    leaseDuration: TimeSpan.Zero));

            Assert.Equal(
                expected: "Provisioning lease duration must be greater than zero.",
                actual: exception.Message);
        }

        [Fact]
        public void Archive_WhenCityIsNotArchived_ArchivesCityAndClearsLease()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            city.TryAcquireProvisioningLease(
                acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
                leaseDuration: TimeSpan.FromMinutes(10));
            city.ClearDomainEvents();

            city.Archive(CityTestData.ArchivedAtUtc);

            Assert.Equal(
                expected: CityStatus.Archived,
                actual: city.Status);
            Assert.True(city.IsArchived);
            Assert.Equal(
                expected: CityTestData.ArchivedAtUtc,
                actual: city.ArchivedAtUtc);
            Assert.Null(city.ProvisioningHeartbeatAtUtc);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void Archive_WhenAlreadyArchived_IsNoOp()
        {
            City city = CityTestData.CreateCity();

            city.Archive(CityTestData.ArchivedAtUtc);
            city.ClearDomainEvents();

            city.Archive(CityTestData.ArchivedAtUtc.AddDays(1));

            Assert.Equal(
                expected: CityTestData.ArchivedAtUtc,
                actual: city.ArchivedAtUtc);
            Assert.Empty(city.DomainEvents);
        }
    }
}

using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class CityLeaseAndArchiveTests
{
    [Fact]
    public void TryAcquireProvisioningLease_WhenProvisioningAndLeaseIsFree_AcquiresLease()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        var leaseDuration = TimeSpan.FromMinutes(10);

        city.ClearDomainEvents();

        var result = city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: leaseDuration);

        Assert.True(result);
        Assert.Equal(CityStatus.Provisioning, city.Status);
        Assert.Equal(CityTestData.CreatedAtUtc, city.ProvisioningStartedAtUtc);
        Assert.Equal(CityTestData.LeaseAcquiredAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(CityTestData.LeaseAcquiredAtUtc.Add(leaseDuration), city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(1, city.ProvisioningAttemptCount);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryAcquireProvisioningLease_WhenCityIsNotProvisioning_ReturnsFalse()
    {
        var city = CityTestData.CreateCity();

        city.ClearDomainEvents();

        var result = city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: TimeSpan.FromMinutes(10));

        Assert.False(result);
        Assert.Equal(CityStatus.Active, city.Status);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(0, city.ProvisioningAttemptCount);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryAcquireProvisioningLease_WhenLeaseIsStillActive_ReturnsFalseWithoutIncrementingAttempts()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        var leaseDuration = TimeSpan.FromMinutes(10);

        city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: leaseDuration);
        city.ClearDomainEvents();

        var result = city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc.AddMinutes(5),
            leaseDuration: leaseDuration);

        Assert.False(result);
        Assert.Equal(CityTestData.LeaseAcquiredAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(CityTestData.LeaseAcquiredAtUtc.Add(leaseDuration), city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(1, city.ProvisioningAttemptCount);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryAcquireProvisioningLease_WithNonPositiveDuration_ThrowsInvalidOperationException()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        var exception = Assert.Throws<InvalidOperationException>(() => city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: TimeSpan.Zero));

        Assert.Equal("Provisioning lease duration must be greater than zero.", exception.Message);
    }

    [Fact]
    public void TryRefreshProvisioningLease_WhenLeaseIsActive_RefreshesHeartbeatAndExpiration()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        var leaseDuration = TimeSpan.FromMinutes(10);

        city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: leaseDuration);
        city.ClearDomainEvents();

        var result = city.TryRefreshProvisioningLease(
            heartbeatAtUtc: CityTestData.LeaseHeartbeatAtUtc,
            leaseDuration: leaseDuration);

        Assert.True(result);
        Assert.Equal(CityTestData.LeaseHeartbeatAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(CityTestData.LeaseHeartbeatAtUtc.Add(leaseDuration), city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(1, city.ProvisioningAttemptCount);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryRefreshProvisioningLease_WhenLeaseHasExpired_ReturnsFalse()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        var leaseDuration = TimeSpan.FromMinutes(10);

        city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: leaseDuration);
        city.ClearDomainEvents();

        var result = city.TryRefreshProvisioningLease(
            heartbeatAtUtc: CityTestData.LeaseAcquiredAtUtc.AddMinutes(11),
            leaseDuration: leaseDuration);

        Assert.False(result);
        Assert.Equal(CityTestData.LeaseAcquiredAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(CityTestData.LeaseAcquiredAtUtc.Add(leaseDuration), city.ProvisioningLeaseExpiresAtUtc);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryRefreshProvisioningLease_WithNonPositiveDuration_ThrowsInvalidOperationException()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        var exception = Assert.Throws<InvalidOperationException>(() => city.TryRefreshProvisioningLease(
            heartbeatAtUtc: CityTestData.LeaseHeartbeatAtUtc,
            leaseDuration: TimeSpan.Zero));

        Assert.Equal("Provisioning lease duration must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Archive_WhenCityIsNotArchived_ArchivesCity_ClearsLease_AndEmitsEvent()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        city.TryAcquireProvisioningLease(
            acquiredAtUtc: CityTestData.LeaseAcquiredAtUtc,
            leaseDuration: TimeSpan.FromMinutes(10));
        city.ClearDomainEvents();

        city.Archive(CityTestData.ArchivedAtUtc);

        Assert.Equal(CityStatus.Archived, city.Status);
        Assert.True(city.IsArchived);
        Assert.Equal(CityTestData.ArchivedAtUtc, city.ArchivedAtUtc);
        Assert.Null(city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);

        var archivedEvent = Assert.IsType<CityArchivedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, archivedEvent.CityId);
        Assert.Equal(CityTestData.ArchivedAtUtc, archivedEvent.ArchivedAtUtc);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_IsNoOp()
    {
        var city = CityTestData.CreateCity();

        city.Archive(CityTestData.ArchivedAtUtc);
        city.ClearDomainEvents();

        city.Archive(CityTestData.ArchivedAtUtc.AddDays(1));

        Assert.Equal(CityTestData.ArchivedAtUtc, city.ArchivedAtUtc);
        Assert.Empty(city.DomainEvents);
    }
}

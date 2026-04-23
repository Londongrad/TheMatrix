using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class CityProvisioningTests
{
    [Fact]
    public void TryCompletePopulationBootstrap_WhenOnlyPopulationBootstrapRemains_ActivatesCity_AndEmitsEvent()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: false);

        city.ClearDomainEvents();

        var result = city.TryCompletePopulationBootstrap(
            operationId: city.PopulationBootstrapOperationId,
            completedAtUtc: CityTestData.CompletedAtUtc);

        Assert.True(result);
        Assert.Equal(CityStatus.Active, city.Status);
        Assert.True(city.IsActive);
        Assert.Equal(CityTestData.CompletedAtUtc, city.PopulationBootstrapCompletedAtUtc);
        Assert.Equal(CityTestData.CreatedAtUtc, city.EconomyBootstrapCompletedAtUtc);
        Assert.Null(city.PopulationBootstrapFailedAtUtc);
        Assert.Null(city.PopulationBootstrapFailureCode);
        Assert.Null(city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);

        var completedEvent = Assert.IsType<CityPopulationBootstrapCompletedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, completedEvent.CityId);
        Assert.Equal(city.PopulationBootstrapOperationId, completedEvent.OperationId);
        Assert.Equal(CityTestData.CompletedAtUtc, completedEvent.CompletedAtUtc);
    }

    [Fact]
    public void TryCompletePopulationBootstrap_WhenEconomyBootstrapStillPending_KeepsProvisioning_AndEmitsEvent()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        city.ClearDomainEvents();

        var result = city.TryCompletePopulationBootstrap(
            operationId: city.PopulationBootstrapOperationId,
            completedAtUtc: CityTestData.CompletedAtUtc);

        Assert.True(result);
        Assert.Equal(CityStatus.Provisioning, city.Status);
        Assert.True(city.IsProvisioning);
        Assert.Equal(CityTestData.CompletedAtUtc, city.PopulationBootstrapCompletedAtUtc);
        Assert.Null(city.EconomyBootstrapCompletedAtUtc);

        Assert.IsType<CityPopulationBootstrapCompletedDomainEvent>(Assert.Single(city.DomainEvents));
    }

    [Fact]
    public void TryCompleteEconomyBootstrap_WhenSecondBootstrapCompletes_ActivatesCity_WithoutNewEvent()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        city.ClearDomainEvents();
        city.TryCompletePopulationBootstrap(
            operationId: city.PopulationBootstrapOperationId,
            completedAtUtc: CityTestData.CompletedAtUtc);
        city.ClearDomainEvents();

        var result = city.TryCompleteEconomyBootstrap(
            operationId: city.EconomyBootstrapOperationId,
            completedAtUtc: CityTestData.CompletedAtUtc);

        Assert.True(result);
        Assert.Equal(CityStatus.Active, city.Status);
        Assert.True(city.IsActive);
        Assert.Equal(CityTestData.CompletedAtUtc, city.EconomyBootstrapCompletedAtUtc);
        Assert.Null(city.EconomyBootstrapFailedAtUtc);
        Assert.Null(city.EconomyBootstrapFailureCode);
        Assert.Null(city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryCompletePopulationBootstrap_WithWrongOperationId_ReturnsFalseWithoutChangingState()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: false);

        city.ClearDomainEvents();

        var result = city.TryCompletePopulationBootstrap(
            operationId: Guid.NewGuid(),
            completedAtUtc: CityTestData.CompletedAtUtc);

        Assert.False(result);
        Assert.Equal(CityStatus.Provisioning, city.Status);
        Assert.Null(city.PopulationBootstrapCompletedAtUtc);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryFailPopulationBootstrap_WhenOperationMatches_FailsProvisioning_NormalizesCode_AndEmitsEvent()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        city.ClearDomainEvents();

        var result = city.TryFailPopulationBootstrap(
            operationId: city.PopulationBootstrapOperationId,
            failureCode: "population_seed_invalid",
            failedAtUtc: CityTestData.FailedAtUtc);

        Assert.True(result);
        Assert.Equal(CityStatus.ProvisioningFailed, city.Status);
        Assert.True(city.HasPopulationBootstrapFailure);
        Assert.False(city.HasEconomyBootstrapFailure);
        Assert.Equal(CityTestData.FailedAtUtc, city.PopulationBootstrapFailedAtUtc);
        Assert.Equal("POPULATION_SEED_INVALID", city.PopulationBootstrapFailureCode);
        Assert.Null(city.PopulationBootstrapCompletedAtUtc);
        Assert.Null(city.EconomyBootstrapFailedAtUtc);
        Assert.Null(city.EconomyBootstrapFailureCode);
        Assert.Null(city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);

        var failedEvent = Assert.IsType<CityPopulationBootstrapFailedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, failedEvent.CityId);
        Assert.Equal(city.PopulationBootstrapOperationId, failedEvent.OperationId);
        Assert.Equal("POPULATION_SEED_INVALID", failedEvent.FailureCode);
        Assert.Equal(CityTestData.FailedAtUtc, failedEvent.FailedAtUtc);
    }

    [Fact]
    public void TryFailEconomyBootstrap_WhenOperationMatches_FailsProvisioning_AndNormalizesCode()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        city.ClearDomainEvents();

        var result = city.TryFailEconomyBootstrap(
            operationId: city.EconomyBootstrapOperationId,
            failureCode: "economy_pipeline_timeout",
            failedAtUtc: CityTestData.FailedAtUtc);

        Assert.True(result);
        Assert.Equal(CityStatus.ProvisioningFailed, city.Status);
        Assert.False(city.HasPopulationBootstrapFailure);
        Assert.True(city.HasEconomyBootstrapFailure);
        Assert.Equal(CityTestData.FailedAtUtc, city.EconomyBootstrapFailedAtUtc);
        Assert.Equal("ECONOMY_PIPELINE_TIMEOUT", city.EconomyBootstrapFailureCode);
        Assert.Null(city.EconomyBootstrapCompletedAtUtc);
        Assert.Null(city.PopulationBootstrapFailedAtUtc);
        Assert.Null(city.PopulationBootstrapFailureCode);
        Assert.Null(city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void TryRestartPopulationBootstrap_WhenProvisioningFailed_ResetsProvisioningState_AndEmitsEvent()
    {
        var city = CityTestData.CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);

        city.ClearDomainEvents();
        city.TryFailPopulationBootstrap(
            operationId: city.PopulationBootstrapOperationId,
            failureCode: "population_seed_invalid",
            failedAtUtc: CityTestData.FailedAtUtc);

        var previousPopulationOperationId = city.PopulationBootstrapOperationId;
        var previousEconomyOperationId = city.EconomyBootstrapOperationId;

        city.ClearDomainEvents();

        var result = city.TryRestartPopulationBootstrap(
            restartedAtUtc: CityTestData.RestartedAtUtc,
            plannedPeopleCountOverride: 50_000,
            out var populationOperationId,
            out var economyOperationId);

        Assert.True(result);
        Assert.Equal(CityStatus.Provisioning, city.Status);
        Assert.True(city.IsProvisioning);
        Assert.Equal(populationOperationId, city.PopulationBootstrapOperationId);
        Assert.Equal(economyOperationId, city.EconomyBootstrapOperationId);
        Assert.NotEqual(previousPopulationOperationId, populationOperationId);
        Assert.NotEqual(previousEconomyOperationId, economyOperationId);
        Assert.Equal(50_000, city.GenerationProfile.PlannedPeopleCount);
        Assert.Null(city.PopulationBootstrapCompletedAtUtc);
        Assert.Null(city.EconomyBootstrapCompletedAtUtc);
        Assert.Null(city.PopulationBootstrapFailedAtUtc);
        Assert.Null(city.EconomyBootstrapFailedAtUtc);
        Assert.Null(city.PopulationBootstrapFailureCode);
        Assert.Null(city.EconomyBootstrapFailureCode);
        Assert.Equal(CityTestData.RestartedAtUtc, city.ProvisioningStartedAtUtc);
        Assert.Equal(CityTestData.RestartedAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(0, city.ProvisioningAttemptCount);

        var restartedEvent = Assert.IsType<CityPopulationBootstrapRestartedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, restartedEvent.CityId);
        Assert.Equal(previousPopulationOperationId, restartedEvent.PreviousOperationId);
        Assert.Equal(populationOperationId, restartedEvent.OperationId);
        Assert.Equal(CityTestData.RestartedAtUtc, restartedEvent.RestartedAtUtc);
    }
}

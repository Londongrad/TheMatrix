using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityProvisioningTests
    {
        [Fact]
        public void TryCompletePopulationBootstrap_WhenOnlyPopulationBootstrapRemains_ActivatesCity_AndEmitsEvent()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: false);

            city.ClearDomainEvents();

            bool result = city.TryCompletePopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                completedAtUtc: CityTestData.CompletedAtUtc);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.Active,
                actual: city.Status);
            Assert.True(city.IsActive);
            Assert.Equal(
                expected: CityTestData.CompletedAtUtc,
                actual: city.PopulationBootstrapCompletedAtUtc);
            Assert.Equal(
                expected: CityTestData.CreatedAtUtc,
                actual: city.EconomyBootstrapCompletedAtUtc);
            Assert.Null(city.PopulationBootstrapFailedAtUtc);
            Assert.Null(city.PopulationBootstrapFailureCode);
            Assert.Null(city.ProvisioningHeartbeatAtUtc);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);

            CityPopulationBootstrapCompletedDomainEvent completedEvent =
                Assert.IsType<CityPopulationBootstrapCompletedDomainEvent>(Assert.Single(city.DomainEvents));

            Assert.Equal(
                expected: city.Id,
                actual: completedEvent.CityId);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: completedEvent.OperationId);
            Assert.Equal(
                expected: CityTestData.CompletedAtUtc,
                actual: completedEvent.CompletedAtUtc);
        }

        [Fact]
        public void TryCompletePopulationBootstrap_WhenEconomyBootstrapStillPending_KeepsProvisioning_AndEmitsEvent()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            city.ClearDomainEvents();

            bool result = city.TryCompletePopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                completedAtUtc: CityTestData.CompletedAtUtc);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.Provisioning,
                actual: city.Status);
            Assert.True(city.IsProvisioning);
            Assert.Equal(
                expected: CityTestData.CompletedAtUtc,
                actual: city.PopulationBootstrapCompletedAtUtc);
            Assert.Null(city.EconomyBootstrapCompletedAtUtc);

            Assert.IsType<CityPopulationBootstrapCompletedDomainEvent>(Assert.Single(city.DomainEvents));
        }

        [Fact]
        public void TryCompleteEconomyBootstrap_WhenSecondBootstrapCompletes_ActivatesCity_WithoutNewEvent()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            city.ClearDomainEvents();
            city.TryCompletePopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                completedAtUtc: CityTestData.CompletedAtUtc);
            city.ClearDomainEvents();

            bool result = city.TryCompleteEconomyBootstrap(
                operationId: city.EconomyBootstrapOperationId,
                completedAtUtc: CityTestData.CompletedAtUtc);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.Active,
                actual: city.Status);
            Assert.True(city.IsActive);
            Assert.Equal(
                expected: CityTestData.CompletedAtUtc,
                actual: city.EconomyBootstrapCompletedAtUtc);
            Assert.Null(city.EconomyBootstrapFailedAtUtc);
            Assert.Null(city.EconomyBootstrapFailureCode);
            Assert.Null(city.ProvisioningHeartbeatAtUtc);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryCompletePopulationBootstrap_WithWrongOperationId_ReturnsFalseWithoutChangingState()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: false);

            city.ClearDomainEvents();

            bool result = city.TryCompletePopulationBootstrap(
                operationId: Guid.NewGuid(),
                completedAtUtc: CityTestData.CompletedAtUtc);

            Assert.False(result);
            Assert.Equal(
                expected: CityStatus.Provisioning,
                actual: city.Status);
            Assert.Null(city.PopulationBootstrapCompletedAtUtc);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void TryFailPopulationBootstrap_WhenOperationMatches_FailsProvisioning_NormalizesCode_AndEmitsEvent()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            city.ClearDomainEvents();

            bool result = city.TryFailPopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                failureCode: "population_seed_invalid",
                failedAtUtc: CityTestData.FailedAtUtc);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.ProvisioningFailed,
                actual: city.Status);
            Assert.True(city.HasPopulationBootstrapFailure);
            Assert.False(city.HasEconomyBootstrapFailure);
            Assert.Equal(
                expected: CityTestData.FailedAtUtc,
                actual: city.PopulationBootstrapFailedAtUtc);
            Assert.Equal(
                expected: "POPULATION_SEED_INVALID",
                actual: city.PopulationBootstrapFailureCode);
            Assert.Null(city.PopulationBootstrapCompletedAtUtc);
            Assert.Null(city.EconomyBootstrapFailedAtUtc);
            Assert.Null(city.EconomyBootstrapFailureCode);
            Assert.Null(city.ProvisioningHeartbeatAtUtc);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);

            CityPopulationBootstrapFailedDomainEvent failedEvent =
                Assert.IsType<CityPopulationBootstrapFailedDomainEvent>(Assert.Single(city.DomainEvents));

            Assert.Equal(
                expected: city.Id,
                actual: failedEvent.CityId);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: failedEvent.OperationId);
            Assert.Equal(
                expected: "POPULATION_SEED_INVALID",
                actual: failedEvent.FailureCode);
            Assert.Equal(
                expected: CityTestData.FailedAtUtc,
                actual: failedEvent.FailedAtUtc);
        }

        [Fact]
        public void TryFailEconomyBootstrap_WhenOperationMatches_FailsProvisioning_AndNormalizesCode()
        {
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            city.ClearDomainEvents();

            bool result = city.TryFailEconomyBootstrap(
                operationId: city.EconomyBootstrapOperationId,
                failureCode: "economy_pipeline_timeout",
                failedAtUtc: CityTestData.FailedAtUtc);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.ProvisioningFailed,
                actual: city.Status);
            Assert.False(city.HasPopulationBootstrapFailure);
            Assert.True(city.HasEconomyBootstrapFailure);
            Assert.Equal(
                expected: CityTestData.FailedAtUtc,
                actual: city.EconomyBootstrapFailedAtUtc);
            Assert.Equal(
                expected: "ECONOMY_PIPELINE_TIMEOUT",
                actual: city.EconomyBootstrapFailureCode);
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
            City city = CityTestData.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);

            city.ClearDomainEvents();
            city.TryFailPopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                failureCode: "population_seed_invalid",
                failedAtUtc: CityTestData.FailedAtUtc);

            Guid previousPopulationOperationId = city.PopulationBootstrapOperationId;
            Guid previousEconomyOperationId = city.EconomyBootstrapOperationId;

            city.ClearDomainEvents();

            bool result = city.TryRestartPopulationBootstrap(
                restartedAtUtc: CityTestData.RestartedAtUtc,
                plannedPeopleCountOverride: 50_000,
                populationOperationId: out Guid populationOperationId,
                economyOperationId: out Guid economyOperationId);

            Assert.True(result);
            Assert.Equal(
                expected: CityStatus.Provisioning,
                actual: city.Status);
            Assert.True(city.IsProvisioning);
            Assert.Equal(
                expected: populationOperationId,
                actual: city.PopulationBootstrapOperationId);
            Assert.Equal(
                expected: economyOperationId,
                actual: city.EconomyBootstrapOperationId);
            Assert.NotEqual(
                expected: previousPopulationOperationId,
                actual: populationOperationId);
            Assert.NotEqual(
                expected: previousEconomyOperationId,
                actual: economyOperationId);
            Assert.Equal(
                expected: 50_000,
                actual: city.GenerationProfile.PlannedPeopleCount);
            Assert.Null(city.PopulationBootstrapCompletedAtUtc);
            Assert.Null(city.EconomyBootstrapCompletedAtUtc);
            Assert.Null(city.PopulationBootstrapFailedAtUtc);
            Assert.Null(city.EconomyBootstrapFailedAtUtc);
            Assert.Null(city.PopulationBootstrapFailureCode);
            Assert.Null(city.EconomyBootstrapFailureCode);
            Assert.Equal(
                expected: CityTestData.RestartedAtUtc,
                actual: city.ProvisioningStartedAtUtc);
            Assert.Equal(
                expected: CityTestData.RestartedAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 0,
                actual: city.ProvisioningAttemptCount);

            CityPopulationBootstrapRestartedDomainEvent restartedEvent =
                Assert.IsType<CityPopulationBootstrapRestartedDomainEvent>(Assert.Single(city.DomainEvents));

            Assert.Equal(
                expected: city.Id,
                actual: restartedEvent.CityId);
            Assert.Equal(
                expected: previousPopulationOperationId,
                actual: restartedEvent.PreviousOperationId);
            Assert.Equal(
                expected: populationOperationId,
                actual: restartedEvent.OperationId);
            Assert.Equal(
                expected: CityTestData.RestartedAtUtc,
                actual: restartedEvent.RestartedAtUtc);
        }
    }
}

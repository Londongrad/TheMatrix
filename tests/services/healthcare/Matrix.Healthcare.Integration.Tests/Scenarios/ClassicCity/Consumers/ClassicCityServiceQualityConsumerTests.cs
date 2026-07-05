using Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;
using Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityServiceQualityConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_MapsHealthcareQualityToNeutralCommand()
    {
        var mediator = new HealthcareIntegrationMediatorStub();
        var consumer = new ClassicCityServiceQualityConsumer(
            mediator,
            NullLogger<ClassicCityServiceQualityConsumer>.Instance);
        var message = new ClassicCityServiceQualitySnapshotV1(
            CityId: Guid.NewGuid(),
            HealthcareQualityIndex: 0.82m,
            EducationQualityIndex: 0.91m,
            HousingSupportIndex: 0.74m,
            OccurredAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"));

        await consumer.ConsumeAsync(message, CancellationToken.None);

        SynchronizeCareServiceQualityCommand command = Assert.Single(
            mediator.CareQualityCommands);
        Assert.Equal(message.CityId, command.SimulationHostId);
        Assert.Equal(message.HealthcareQualityIndex, command.QualityMultiplier);
        Assert.Equal(message.OccurredAtUtc, command.ObservedAtUtc);
    }

    [Fact]
    public void EndpointConstants_AreStableAndBoundConcurrency()
    {
        Assert.Equal(
            "healthcare-classic-city-service-quality-v1",
            ClassicCityServiceQualityConsumerDefinition.EndpointNameValue);
        Assert.Equal(
            4,
            ClassicCityServiceQualityConsumerDefinition.ConcurrentMessageLimitValue);
    }
}

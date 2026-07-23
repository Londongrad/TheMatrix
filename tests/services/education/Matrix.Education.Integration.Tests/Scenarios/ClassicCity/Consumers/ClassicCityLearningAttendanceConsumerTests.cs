using Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Xunit;

namespace Matrix.Education.Integration.Tests.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityLearningAttendanceConsumerTests
{
    [Fact]
    public void Map_ResolvesSharedAreaAndPreservesVersionAndTime()
    {
        var message = CreateMessage();
        var command = ClassicCityLearningAttendanceConsumer.Map(message);
        Assert.Equal(message.SourceTickId, command.SourceTickId);
        Assert.Equal(message.ObservedAtSimTimeUtc, command.ObservedAtSimTimeUtc);
        var resident = Assert.Single(command.Residents);
        Assert.Equal(3, resident.ParticipationRevision);
        Assert.Equal(2, resident.LifecycleRevision);
        Assert.Equal(0.7m, resident.Conditions.RoadAccessibility);
        Assert.Equal(25, resident.Conditions.Energy);
    }

    [Fact]
    public void Map_RejectsInvalidAreaReferenceAndBatchNumber()
    {
        var message = CreateMessage();
        Assert.Throws<ArgumentException>(() => ClassicCityLearningAttendanceConsumer.Map(message with { BatchNumber = 0 }));
        Assert.Throws<ArgumentException>(() => ClassicCityLearningAttendanceConsumer.Map(message with
            { Residents = [message.Residents[0] with { AreaIndex = 1 }] }));
    }

    private static ClassicCityResidentActivityConditionsBatchV1 CreateMessage() => new(Guid.NewGuid(), 5,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, 1,
        [new(null, 0.7m, 1m, 1m, 1m, 0m, 0m, 0m, false)],
        [new(Guid.NewGuid(), 2, 3, 0, 18, 25, 10, 100, false, false, true, 1m)]);
}

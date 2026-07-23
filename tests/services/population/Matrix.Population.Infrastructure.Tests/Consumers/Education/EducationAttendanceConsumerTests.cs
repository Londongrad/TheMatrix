using Matrix.Education.Contracts.Events;
using Matrix.Population.Infrastructure.Consumers.Education;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Consumers.Education;

public sealed class EducationAttendanceConsumerTests
{
    [Fact]
    public void Map_PreservesAttendanceIdentityAndRejectsNullRows()
    {
        var resident = new EducationAttendanceEvaluatedV1(Guid.NewGuid(), 2, 3, 0.73m, 0.84m);
        var message = new EducationAttendanceEvaluatedBatchV1(Guid.NewGuid(), 5, DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow, [resident]);
        var command = EducationAttendanceConsumer.Map(message);
        Assert.Equal(message.SimulationHostId, command.SimulationHostId);
        Assert.Equal(message.SourceTickId, command.SourceTickId);
        Assert.Equal(message.ObservedAtSimTimeUtc, command.ObservedAtSimTimeUtc);
        var mapped = Assert.Single(command.Residents);
        Assert.Equal(resident.ResidentLifecycleRevision, mapped.ResidentLifecycleRevision);
        Assert.Equal(resident.ParticipationRevision, mapped.ParticipationRevision);
        Assert.Equal(resident.AttendanceIndex, mapped.AttendanceIndex);
        Assert.Equal(resident.CommuteAccessibilityIndex, mapped.CommuteAccessibilityIndex);
        Assert.Throws<ArgumentException>(() => EducationAttendanceConsumer.Map(message with { Residents = [null!] }));
    }
}

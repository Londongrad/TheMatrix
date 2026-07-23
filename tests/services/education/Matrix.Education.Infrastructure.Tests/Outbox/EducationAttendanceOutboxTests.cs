using System.Text.Json;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Outbox;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Outbox;

public sealed class EducationAttendanceOutboxTests
{
    [Fact]
    public async Task AttendanceAndOutbox_RoundTripTogetherWithOriginalVersionsAndSimulationTime()
    {
        await using var db = EducationInfrastructureTestSupport.CreateDbContext();
        var observedAt = new DateTimeOffset(2048, 5, 2, 9, 0, 0, TimeSpan.Zero);
        var profile = StudentProfile.Register(new ResidentId(Guid.NewGuid()), new SimulationHostId(Guid.NewGuid()),
            new DateOnly(2030, 1, 1), true, true, 1, observedAt);
        profile.RecordParticipationChange();
        db.StudentProfiles.Add(profile);
        Assert.True(profile.TryRecordAttendance(5, 1, 0, observedAt, 0.73m, 0.84m));
        var result = new EducationAttendanceEvaluatedV1(profile.ResidentId.Value, 0, 1, 0.73m, 0.84m);
        await new EducationAttendanceOutboxWriter(db).AddAsync(new(profile.SimulationHostId.Value, 5, observedAt,
            DateTimeOffset.UtcNow, [result]), default);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var restored = await db.StudentProfiles.SingleAsync();
        Assert.Equal(5, restored.LastAttendanceSourceTickId);
        Assert.Equal(observedAt, restored.AttendanceObservedAtSimTimeUtc);
        Assert.Equal(0.73m, restored.AttendanceIndex);
        Assert.Equal(0.84m, restored.CommuteAccessibilityIndex);
        var message = await db.OutboxMessages.SingleAsync();
        Assert.Equal(EducationOutboxEventTypes.AttendanceEvaluatedBatchV1, message.Type);
        var batch = JsonSerializer.Deserialize<EducationAttendanceEvaluatedBatchV1>(message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(result, Assert.Single(batch.Residents));
        Assert.Equal(observedAt, batch.ObservedAtSimTimeUtc);
        Assert.Equal(5, batch.SourceTickId);
        Assert.Equal(profile.SimulationHostId.Value, batch.SimulationHostId);
    }
}

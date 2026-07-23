using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Domain.Tests.Students;

public sealed class StudentAttendanceTests
{
    [Fact]
    public void Attendance_RejectsOldTicksAndInvalidatedParticipation()
    {
        var now = new DateTimeOffset(2048, 5, 3, 9, 0, 0, TimeSpan.Zero);
        var profile = StudentProfile.Register(new ResidentId(Guid.NewGuid()), new SimulationHostId(Guid.NewGuid()),
            new DateOnly(2030, 1, 1), true, true, 1, now);
        profile.RecordParticipationChange();
        Assert.True(profile.TryRecordAttendance(5, 1, 0, now, 0.8m, 0.9m));
        Assert.False(profile.TryRecordAttendance(5, 1, 0, now, 0.1m, 0.1m));
        Assert.False(profile.TryRecordAttendance(4, 1, 0, now, 0.1m, 0.1m));
        Assert.False(profile.TryRecordAttendance(6, 1, 0, now.AddHours(-1), 0.1m, 0.1m));
        Assert.Equal(0.8m, profile.AttendanceIndex);
        profile.RecordParticipationChange();
        Assert.Null(profile.AttendanceIndex);
        Assert.False(profile.TryRecordAttendance(6, 1, 0, now, 0.5m, 0.5m));
        Assert.True(profile.TryRecordAttendance(6, 2, 0, now, 0.5m, 0.5m));
        profile.TrySynchronizeResidentFacts(profile.SimulationHostId, profile.BirthDate, false, true, 2, now, 1);
        Assert.Null(profile.AttendanceIndex);
        Assert.False(profile.TryRecordAttendance(7, 2, 0, now, 1m, 1m));
    }
}

using Matrix.Education.Contracts.Events;
using Matrix.Population.Domain.Models;
using Matrix.Population.Infrastructure.Integration.Education;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Integration;

public sealed class EducationRoutineMapperTests
{
    [Fact]
    public void RoundTrip_PreservesHoursDaysLoadAndExplicitAbsence()
    {
        var profile = EducationRoutineMapper.FromContract(new(new(615, 1440, 65, "demanding")));
        Assert.Equal(TimeSpan.FromMinutes(615), profile.StructuredActivityStart);
        Assert.Equal(TimeSpan.FromDays(1), profile.StructuredActivityEnd);
        Assert.Equal(PersonRoutineDays.Saturday | PersonRoutineDays.Sunday, profile.ActivityDays);
        Assert.Equal(PersonStructuredActivityLoad.Demanding, profile.StructuredActivityLoad);
        Assert.Equal(profile, EducationRoutineMapper.Deserialize(EducationRoutineMapper.Serialize(profile)));
        Assert.Same(PersonRoutineProfile.Unstructured, EducationRoutineMapper.FromContract(new(null)));
        Assert.Same(PersonRoutineProfile.Unstructured,
            EducationRoutineMapper.Deserialize(EducationRoutineMapper.Serialize(PersonRoutineProfile.Unstructured)));
    }

    [Theory]
    [InlineData(-1, 900, 62, "moderate")]
    [InlineData(480, 480, 62, "moderate")]
    [InlineData(480, 1441, 62, "moderate")]
    [InlineData(1440, 1440, 62, "moderate")]
    [InlineData(480, 900, 0, "moderate")]
    [InlineData(480, 900, 128, "moderate")]
    [InlineData(480, 900, -1, "moderate")]
    [InlineData(480, 900, 62, "unknown")]
    public void FromContract_RejectsMalformedActivity(int start, int end, int days, string load) =>
        Assert.ThrowsAny<ArgumentException>(() => EducationRoutineMapper.FromContract(new(new(start, end, days, load))));

    [Fact]
    public void Serialize_DoesNotSilentlyTruncateInternalPrecision()
    {
        var profile = PersonRoutineProfile.Structured(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1), PersonStructuredActivityLoad.Moderate);
        Assert.Throws<ArgumentException>(() => EducationRoutineMapper.Serialize(profile));
        Assert.Throws<InvalidOperationException>(() => EducationRoutineMapper.Deserialize("null"));
    }
}

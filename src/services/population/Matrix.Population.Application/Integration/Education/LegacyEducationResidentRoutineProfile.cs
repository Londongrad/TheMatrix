using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration.Education;

// Compatibility for persisted projections and participation events written before DailyRoutine existed.
internal static class LegacyEducationResidentRoutineProfile
{
    internal static PersonRoutineProfile Enrolled { get; } = PersonRoutineProfile.Structured(
        TimeSpan.FromHours(8), TimeSpan.FromHours(15), PersonStructuredActivityLoad.Moderate);
}

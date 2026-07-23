using System.Text.Json;
using Matrix.Education.Contracts.Events;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Infrastructure.Integration.Education;

internal static class EducationRoutineMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static PersonRoutineProfile FromContract(EducationDailyRoutineV1 routine)
    {
        ArgumentNullException.ThrowIfNull(routine);
        if (routine.StructuredActivity is not { } activity)
            return PersonRoutineProfile.Unstructured;
        if (activity.StartMinuteOfDay is < 0 or >= 1440 || activity.EndMinuteOfDay <= activity.StartMinuteOfDay
            || activity.EndMinuteOfDay > 1440)
            throw new ArgumentException("Education activity must be a nonempty interval within one local day.", nameof(routine));
        var load = activity.Load switch
        {
            "moderate" => PersonStructuredActivityLoad.Moderate,
            "demanding" => PersonStructuredActivityLoad.Demanding,
            _ => throw new ArgumentException("Unsupported education activity load.", nameof(routine))
        };
        return PersonRoutineProfile.Structured(TimeSpan.FromMinutes(activity.StartMinuteOfDay),
            TimeSpan.FromMinutes(activity.EndMinuteOfDay), load, (PersonRoutineDays)activity.DaysOfWeekMask);
    }

    internal static string Serialize(PersonRoutineProfile routine)
    {
        EducationScheduledActivityV1? activity = null;
        if (routine.HasStructuredActivity)
        {
            // The integration contract has minute precision; never truncate a richer internal profile.
            if (routine.StructuredActivityStart!.Value.Ticks % TimeSpan.TicksPerMinute != 0
                || routine.StructuredActivityEnd!.Value.Ticks % TimeSpan.TicksPerMinute != 0)
                throw new ArgumentException("Education routine times must use whole minutes.", nameof(routine));
            activity = new((int)routine.StructuredActivityStart.Value.TotalMinutes,
                (int)routine.StructuredActivityEnd.Value.TotalMinutes, (int)routine.ActivityDays,
                routine.StructuredActivityLoad == PersonStructuredActivityLoad.Moderate ? "moderate" : "demanding");
        }
        return JsonSerializer.Serialize(new EducationDailyRoutineV1(activity), JsonOptions);
    }

    internal static PersonRoutineProfile Deserialize(string json) => FromContract(
        JsonSerializer.Deserialize<EducationDailyRoutineV1>(json, JsonOptions)
        ?? throw new InvalidOperationException("Stored education routine cannot be null."));
}

namespace Matrix.Education.Contracts.Events;

// Minutes and weekdays are expressed in the simulation host's local time.
// Weekday bits follow DayOfWeek: Sunday = 1, Monday = 2, ... Saturday = 64.
public sealed record EducationScheduledActivityV1(
    int StartMinuteOfDay, int EndMinuteOfDay, int DaysOfWeekMask, string Load);

// An explicit null activity means no scheduled attendance; an absent routine on
// the participation event identifies the older version of its payload.
public sealed record EducationDailyRoutineV1(EducationScheduledActivityV1? StructuredActivity);

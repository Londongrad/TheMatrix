using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration
{
    public static class PersonRoutineProfileFactory
    {
        private static readonly PersonRoutineProfile EmploymentRoutine = PersonRoutineProfile.Structured(
            activityStart: TimeSpan.FromHours(8),
            activityEnd: TimeSpan.FromHours(17),
            activityLoad: PersonStructuredActivityLoad.Demanding);

        public static PersonRoutineProfile Create(
            Person resident,
            ResidentExternalActivityProfile? externalActivity)
        {
            ArgumentNullException.ThrowIfNull(resident);

            if (resident.Employment.Status == EmploymentStatus.Employed)
                return EmploymentRoutine;

            return externalActivity?.ResidentLifecycleRevision == resident.LifecycleRevision
                ? externalActivity.Routine
                : PersonRoutineProfile.Unstructured;
        }
    }
}

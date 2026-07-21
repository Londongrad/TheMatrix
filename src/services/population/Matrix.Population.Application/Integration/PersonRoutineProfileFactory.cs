using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration
{
    public static class PersonRoutineProfileFactory
    {
        private static readonly TimeSpan ActivityStart = TimeSpan.FromHours(8);
        private static readonly TimeSpan ExternalActivityEnd = TimeSpan.FromHours(15);
        private static readonly TimeSpan EmploymentActivityEnd = TimeSpan.FromHours(17);

        public static PersonRoutineProfile Create(
            Person resident,
            ResidentExternalActivityProfile? externalActivity)
        {
            ArgumentNullException.ThrowIfNull(resident);

            if (resident.Employment.Status == EmploymentStatus.Employed)
                return PersonRoutineProfile.Structured(
                    activityStart: ActivityStart,
                    activityEnd: EmploymentActivityEnd,
                    activityLoad: PersonStructuredActivityLoad.Demanding);

            return externalActivity?.HasStructuredActivity == true
                ? PersonRoutineProfile.Structured(
                    activityStart: ActivityStart,
                    activityEnd: ExternalActivityEnd,
                    activityLoad: PersonStructuredActivityLoad.Moderate)
                : PersonRoutineProfile.Unstructured;
        }
    }
}

using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentRoutineProfileFactory
    {
        private static readonly TimeSpan ActivityStart = TimeSpan.FromHours(8);
        private static readonly TimeSpan EducationActivityEnd = TimeSpan.FromHours(15);
        private static readonly TimeSpan EmploymentActivityEnd = TimeSpan.FromHours(17);

        internal static PersonRoutineProfile Create(
            Person resident,
            EducationParticipationProjection? educationParticipation)
        {
            if (resident.Employment.Status == EmploymentStatus.Employed)
                return PersonRoutineProfile.Structured(
                    activityStart: ActivityStart,
                    activityEnd: EmploymentActivityEnd,
                    activityLoad: PersonStructuredActivityLoad.Demanding);

            return educationParticipation?.IsEnrolled == true
                ? PersonRoutineProfile.Structured(
                    activityStart: ActivityStart,
                    activityEnd: EducationActivityEnd,
                    activityLoad: PersonStructuredActivityLoad.Moderate)
                : PersonRoutineProfile.Unstructured;
        }
    }
}

using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class ClassicCityActivityFactory
    {
        public static CityPopulationActivityWriteModel PopulationInitialized(
            Guid cityId,
            DateOnly currentDate,
            int requestedPeopleCount,
            int generatedPeopleCount,
            int householdCount)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                EventType: CityPopulationActivityEventType.PopulationInitialized,
                Source: CityPopulationActivitySource.Bootstrap,
                Severity: CityPopulationActivitySeverity.Success,
                Title: "Population initialized",
                Summary:
                $"Bootstrap created {generatedPeopleCount} residents across {householdCount} households (requested {requestedPeopleCount}).");
        }

        public static CityPopulationActivityWriteModel ResidentHired(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            string jobTitle = resident.Employment.Job?.Title ?? "worker";

            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.ResidentHired,
                title: "Resident hired",
                summary: $"{resident.Name} started working as {jobTitle}.");
        }

        public static CityPopulationActivityWriteModel ResidentFired(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            string? previousJobTitle,
            CityPopulationActivitySource source)
        {
            string jobTitle = string.IsNullOrWhiteSpace(previousJobTitle)
                ? "their job"
                : previousJobTitle;

            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.ResidentFired,
                title: "Resident left employment",
                summary: $"{resident.Name} left {jobTitle} work and became unemployed.");
        }

        public static CityPopulationActivityWriteModel ResidentRetired(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.ResidentRetired,
                title: "Resident retired",
                summary: $"{resident.Name} retired from active employment.");
        }

        public static CityPopulationActivityWriteModel ResidentEnrolled(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.ResidentEnrolled,
                title: "Resident enrolled",
                summary: $"{resident.Name} started studying at {HumanizeEducationLevel(resident.EducationLevel)} level.");
        }

        public static CityPopulationActivityWriteModel ResidentGraduated(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.ResidentGraduated,
                title: "Resident advanced in education",
                summary: $"{resident.Name} advanced to {HumanizeEducationLevel(resident.EducationLevel)} education.");
        }

        public static CityPopulationActivityWriteModel ResidentWithdrewFromStudy(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.ResidentWithdrewFromStudy,
                title: "Resident left study",
                summary: $"{resident.Name} is no longer studying.");
        }

        public static CityPopulationActivityWriteModel ResidentsMarried(
            Guid cityId,
            DateOnly currentDate,
            Person firstResident,
            Person secondResident,
            CityPopulationActivitySource source)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                EventType: CityPopulationActivityEventType.ResidentsMarried,
                Source: source,
                Severity: CityPopulationActivitySeverity.Success,
                Title: "Residents married",
                Summary: $"{firstResident.Name} and {secondResident.Name} formed a shared household through marriage.",
                PrimaryResidentId: firstResident.Id.Value,
                SecondaryResidentId: secondResident.Id.Value);
        }

        public static CityPopulationActivityWriteModel ResidentsDivorced(
            Guid cityId,
            DateOnly currentDate,
            Person firstResident,
            Person secondResident,
            CityPopulationActivitySource source)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                EventType: CityPopulationActivityEventType.ResidentsDivorced,
                Source: source,
                Severity: CityPopulationActivitySeverity.Warning,
                Title: "Residents divorced",
                Summary: $"{firstResident.Name} and {secondResident.Name} ended their marriage and separated households.",
                PrimaryResidentId: firstResident.Id.Value,
                SecondaryResidentId: secondResident.Id.Value);
        }

        public static CityPopulationActivityWriteModel ResidentBecameWidowed(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            string deceasedName,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.ResidentBecameWidowed,
                title: "Resident became widowed",
                summary: $"{resident.Name} became widowed after {deceasedName} died.");
        }

        public static CityPopulationActivityWriteModel ResidentDied(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Danger,
                eventType: CityPopulationActivityEventType.ResidentDied,
                title: "Resident died",
                summary: $"{resident.Name} died.");
        }

        public static CityPopulationActivityWriteModel ResidentResurrected(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.ResidentResurrected,
                title: "Resident resurrected",
                summary: $"{resident.Name} was restored to life.");
        }

        private static CityPopulationActivityWriteModel CreateResidentEvent(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            CityPopulationActivitySeverity severity,
            CityPopulationActivityEventType eventType,
            string title,
            string summary)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                EventType: eventType,
                Source: source,
                Severity: severity,
                Title: title,
                Summary: summary,
                PrimaryResidentId: resident.Id.Value);
        }

        private static string HumanizeEducationLevel(EducationLevel level)
        {
            return level switch
            {
                EducationLevel.None => "no formal",
                EducationLevel.Preschool => "preschool",
                EducationLevel.Primary => "primary",
                EducationLevel.LowerSecondary => "lower secondary",
                EducationLevel.UpperSecondary => "upper secondary",
                EducationLevel.Vocational => "vocational",
                EducationLevel.Higher => "higher",
                EducationLevel.Postgraduate => "postgraduate",
                _ => level.ToString()
            };
        }
    }
}

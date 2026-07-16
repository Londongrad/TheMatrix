using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
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
            int householdCount,
            DateTimeOffset occurredAtUtc)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: occurredAtUtc,
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
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
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
                summary: $"{resident.Name} started working as {jobTitle}.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentFired(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            string? previousJobTitle,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
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
                summary: $"{resident.Name} left {jobTitle} work and became unemployed.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentRetired(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.ResidentRetired,
                title: "Resident retired",
                summary: $"{resident.Name} retired from active employment.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentsMarried(
            Guid cityId,
            DateOnly currentDate,
            Person firstResident,
            Person secondResident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: occurredAtUtc,
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
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: occurredAtUtc,
                EventType: CityPopulationActivityEventType.ResidentsDivorced,
                Source: source,
                Severity: CityPopulationActivitySeverity.Warning,
                Title: "Residents divorced",
                Summary:
                $"{firstResident.Name} and {secondResident.Name} ended their marriage and separated households.",
                PrimaryResidentId: firstResident.Id.Value,
                SecondaryResidentId: secondResident.Id.Value);
        }

        public static CityPopulationActivityWriteModel ResidentBecameWidowed(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            string deceasedName,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.ResidentBecameWidowed,
                title: "Resident became widowed",
                summary: $"{resident.Name} became widowed after {deceasedName} died.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentDied(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Danger,
                eventType: CityPopulationActivityEventType.ResidentDied,
                title: "Resident died",
                summary: $"{resident.Name} died.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentResurrected(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.ResidentResurrected,
                title: "Resident resurrected",
                summary: $"{resident.Name} was restored to life.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentBorn(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            Person mother,
            Person? father,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            string householdSummary = father is null
                ? $"{resident.Name} was born into {mother.Name}'s household."
                : $"{resident.Name} was born to {mother.Name} and {father.Name}.";

            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: occurredAtUtc,
                EventType: CityPopulationActivityEventType.ResidentBorn,
                Source: source,
                Severity: CityPopulationActivitySeverity.Success,
                Title: "Resident born",
                Summary: householdSummary,
                PrimaryResidentId: resident.Id.Value,
                SecondaryResidentId: mother.Id.Value);
        }

        public static CityPopulationActivityWriteModel HouseholdFoundHousing(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.HouseholdFoundHousing,
                title: "Household found housing",
                summary: $"{resident.Name}'s household secured housed placement inside the city.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel HouseholdLostHousing(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Warning,
                eventType: CityPopulationActivityEventType.HouseholdLostHousing,
                title: "Household lost housing",
                summary: $"{resident.Name}'s household lost housed placement and became homeless.",
                occurredAtUtc: occurredAtUtc);
        }

        public static CityPopulationActivityWriteModel ResidentFormedIndependentHousehold(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            DateTimeOffset occurredAtUtc)
        {
            return CreateResidentEvent(
                cityId: cityId,
                currentDate: currentDate,
                resident: resident,
                source: source,
                severity: CityPopulationActivitySeverity.Success,
                eventType: CityPopulationActivityEventType.ResidentFormedIndependentHousehold,
                title: "Resident moved out",
                summary: $"{resident.Name} formed an independent household and is now seeking separate housing.",
                occurredAtUtc: occurredAtUtc);
        }

        private static CityPopulationActivityWriteModel CreateResidentEvent(
            Guid cityId,
            DateOnly currentDate,
            Person resident,
            CityPopulationActivitySource source,
            CityPopulationActivitySeverity severity,
            CityPopulationActivityEventType eventType,
            string title,
            string summary,
            DateTimeOffset occurredAtUtc)
        {
            return new CityPopulationActivityWriteModel(
                CityId: cityId,
                CurrentDate: currentDate,
                OccurredAtUtc: occurredAtUtc,
                EventType: eventType,
                Source: source,
                Severity: severity,
                Title: title,
                Summary: summary,
                PrimaryResidentId: resident.Id.Value);
        }

    }
}

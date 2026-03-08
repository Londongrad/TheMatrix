using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationActivityEvent
    {
        private CityPopulationActivityEvent() { }

        private CityPopulationActivityEvent(
            Guid activityEventId,
            CityId cityId,
            DateOnly currentDate,
            DateTimeOffset occurredAtUtc,
            CityPopulationActivityEventType eventType,
            CityPopulationActivitySource source,
            CityPopulationActivitySeverity severity,
            string title,
            string summary,
            PersonId? primaryResidentId,
            PersonId? secondaryResidentId)
        {
            ActivityEventId = activityEventId;
            CityId = cityId;
            CurrentDate = currentDate;
            OccurredAtUtc = occurredAtUtc;
            EventType = eventType;
            Source = source;
            Severity = severity;
            Title = title;
            Summary = summary;
            PrimaryResidentId = primaryResidentId;
            SecondaryResidentId = secondaryResidentId;
        }

        public Guid ActivityEventId { get; private set; }
        public CityId CityId { get; private set; }
        public DateOnly CurrentDate { get; private set; }
        public DateTimeOffset OccurredAtUtc { get; private set; }
        public CityPopulationActivityEventType EventType { get; private set; }
        public CityPopulationActivitySource Source { get; private set; }
        public CityPopulationActivitySeverity Severity { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Summary { get; private set; } = string.Empty;
        public PersonId? PrimaryResidentId { get; private set; }
        public PersonId? SecondaryResidentId { get; private set; }

        public static CityPopulationActivityEvent Create(
            CityId cityId,
            DateOnly currentDate,
            DateTimeOffset occurredAtUtc,
            CityPopulationActivityEventType eventType,
            CityPopulationActivitySource source,
            CityPopulationActivitySeverity severity,
            string title,
            string summary,
            PersonId? primaryResidentId = null,
            PersonId? secondaryResidentId = null)
        {
            return new CityPopulationActivityEvent(
                activityEventId: Guid.NewGuid(),
                cityId: cityId,
                currentDate: currentDate,
                occurredAtUtc: occurredAtUtc,
                eventType: eventType,
                source: source,
                severity: severity,
                title: title,
                summary: summary,
                primaryResidentId: primaryResidentId,
                secondaryResidentId: secondaryResidentId);
        }
    }
}

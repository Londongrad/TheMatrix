using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class ClassicCityWorkplaceBusinessSyncBatchFactory
    {
        public static ClassicCityWorkplaceBusinessSyncBatchV1[] Build(
            Guid cityId,
            IEnumerable<Person> persons,
            string correlationId,
            DateTimeOffset occurredAtUtc,
            int batchSize)
        {
            ClassicCityWorkplaceBusinessSyncItemV1[] items = persons
               .Where(x => x.Employment.Status == EmploymentStatus.Employed && x.Employment.Job is not null)
               .GroupBy(
                    keySelector: x => x.Employment.Job!.WorkplaceId,
                    elementSelector: x => x)
               .OrderBy(x => x.Key.Value)
               .Select(group =>
                {
                    string jobTitle = group.First()
                       .Employment.Job!.Title;
                    WorkplaceId workplaceId = group.Key;

                    return new ClassicCityWorkplaceBusinessSyncItemV1(
                        WorkplaceId: workplaceId.Value,
                        ExternalReferenceCode: BuildExternalReferenceCode(workplaceId),
                        Name: BuildBusinessName(
                            workplaceId: workplaceId,
                            jobTitle: jobTitle),
                        JobTitle: jobTitle,
                        ActiveWorkerCount: group.Count());
                })
               .ToArray();

            if (items.Length == 0)
                return [];

            ClassicCityWorkplaceBusinessSyncBatchV1[] batches = items
               .Chunk(batchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityWorkplaceBusinessSyncBatchV1(
                    CityId: cityId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Workplaces: chunk,
                    CorrelationId: correlationId,
                    OccurredAtUtc: occurredAtUtc))
               .ToArray();

            for (int i = 0; i < batches.Length; i++)
                batches[i] = batches[i] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        public static string BuildExternalReferenceCode(WorkplaceId workplaceId)
        {
            return $"classic-city-workplace:{workplaceId.Value:N}";
        }

        private static string BuildBusinessName(
            WorkplaceId workplaceId,
            string jobTitle)
        {
            string title = string.IsNullOrWhiteSpace(jobTitle)
                ? "Workplace"
                : jobTitle.Trim();
            string shortCode = workplaceId.Value.ToString("N")[..8]
               .ToUpperInvariant();
            return $"{title} employer {shortCode}";
        }
    }
}

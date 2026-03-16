using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationActivityJournalService(PopulationDbContext dbContext)
        : ICityPopulationActivityJournalService
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task RecordAsync(
            CityPopulationActivityWriteModel entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var activityEvent = CityPopulationActivityEvent.Create(
                cityId: CityId.From(entry.CityId),
                currentDate: entry.CurrentDate,
                occurredAtUtc: entry.OccurredAtUtc,
                eventType: entry.EventType,
                source: entry.Source,
                severity: entry.Severity,
                title: entry.Title,
                summary: entry.Summary,
                primaryResidentId: entry.PrimaryResidentId.HasValue
                    ? PersonId.From(entry.PrimaryResidentId.Value)
                    : null,
                secondaryResidentId: entry.SecondaryResidentId.HasValue
                    ? PersonId.From(entry.SecondaryResidentId.Value)
                    : null);

            await _dbContext.CityPopulationActivityEvents.AddAsync(
                entity: activityEvent,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            CityPopulationActivityEvent[] events = await _dbContext.CityPopulationActivityEvents
               .Where(x => x.CityId == cityId)
               .ToArrayAsync(cancellationToken);

            if (events.Length == 0)
                return;

            _dbContext.CityPopulationActivityEvents.RemoveRange(events);
        }
    }
}

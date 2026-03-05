using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationSummaryProjectionService(PopulationDbContext dbContext)
        : ICityPopulationSummaryProjectionService
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task UpdateAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            CancellationToken cancellationToken = default)
        {
            return UpsertAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: null,
                cancellationToken: cancellationToken);
        }

        public Task UpdateAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken = default)
        {
            return UpsertAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: householdPlacements,
                cancellationToken: cancellationToken);
        }

        public async Task RebuildAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default)
        {
            List<Person> persons = await _dbContext.Persons
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (person, _) => person)
               .ToListAsync(cancellationToken);

            await UpsertAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: null,
                cancellationToken: cancellationToken);
        }

        public async Task EnsureExistsAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            bool exists = await _dbContext.CityPopulationSummaryProjections
               .AsNoTracking()
               .AnyAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            if (exists)
                return;

            bool hasPopulationState = await HasAnyPopulationStateAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (!hasPopulationState)
                return;

            await RebuildAsync(
                cityId: cityId,
                currentDate: await ResolveCurrentDateAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken),
                cancellationToken: cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            CityPopulationSummaryProjection? projection = await _dbContext.CityPopulationSummaryProjections
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            if (projection is not null)
                _dbContext.CityPopulationSummaryProjections.Remove(projection);
        }

        private async Task UpsertAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement>? householdPlacements,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ClassicCityHouseholdPlacement> resolvedPlacements = householdPlacements ??
                await _dbContext.ClassicCityHouseholdPlacements
                   .AsNoTracking()
                   .Where(x => x.CityId == cityId)
                   .ToListAsync(cancellationToken);

            CityPopulationSummaryProjection? projection = await _dbContext.CityPopulationSummaryProjections
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            bool isNewProjection = projection is null;
            projection ??= CityPopulationSummaryProjection.Create(
                cityId: cityId,
                currentDate: currentDate,
                updatedAtUtc: DateTimeOffset.UtcNow);

            ApplySnapshot(
                projection: projection,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: resolvedPlacements);

            if (isNewProjection)
                await _dbContext.CityPopulationSummaryProjections.AddAsync(
                    entity: projection,
                    cancellationToken: cancellationToken);
        }

        private static void ApplySnapshot(
            CityPopulationSummaryProjection projection,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements)
        {
            Dictionary<HouseholdId, HousingStatus> housingByHouseholdId = householdPlacements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.HousingStatus);

            Person[] aliveResidents = persons
               .Where(x => x.IsAlive)
               .ToArray();

            projection.Refresh(
                currentDate: currentDate,
                updatedAtUtc: DateTimeOffset.UtcNow,
                householdCount: householdPlacements.Count,
                housedHouseholdCount: householdPlacements.Count(x => x.HousingStatus == HousingStatus.Housed),
                homelessHouseholdCount: householdPlacements.Count(x => x.HousingStatus == HousingStatus.Homeless),
                residentCount: aliveResidents.Length,
                deceasedCount: persons.Count - aliveResidents.Length,
                housedResidentCount: aliveResidents.Count(x =>
                    housingByHouseholdId.TryGetValue(x.HouseholdId, out HousingStatus status) &&
                    status == HousingStatus.Housed),
                homelessResidentCount: aliveResidents.Count(x =>
                    housingByHouseholdId.TryGetValue(x.HouseholdId, out HousingStatus status) &&
                    status == HousingStatus.Homeless),
                childCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Child),
                youthCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Youth),
                adultCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Adult),
                seniorCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Senior),
                employedCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Employed),
                studentCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Student),
                unemployedCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Unemployed),
                retiredCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Retired),
                averageHealth: aliveResidents.Select(x => (decimal?)x.Health.Value).Average(),
                averageHappiness: aliveResidents.Select(x => (decimal?)x.Happiness.Value).Average(),
                averageEnergy: aliveResidents.Select(x => (decimal?)x.Energy.Value).Average(),
                averageStress: aliveResidents.Select(x => (decimal?)x.Stress.Value).Average(),
                averageSocialNeed: aliveResidents.Select(x => (decimal?)x.SocialNeed.Value).Average());
        }

        private async Task<bool> HasAnyPopulationStateAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            if (await _dbContext.ClassicCityHouseholdPlacements
               .AsNoTracking()
               .AnyAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken))
                return true;

            return await _dbContext.CityPopulationEnvironments.AsNoTracking().AnyAsync(x => x.CityId == cityId, cancellationToken) ||
                   await _dbContext.CityPopulationProgressionStates.AsNoTracking().AnyAsync(x => x.CityId == cityId, cancellationToken) ||
                   await _dbContext.CityPopulationWeatherExposureStates.AsNoTracking().AnyAsync(x => x.CityId == cityId, cancellationToken) ||
                   await _dbContext.CityPopulationWeatherImpactStates.AsNoTracking().AnyAsync(x => x.CityId == cityId, cancellationToken) ||
                   await _dbContext.CityPopulationArchiveStates.AsNoTracking().AnyAsync(x => x.CityId == cityId, cancellationToken) ||
                   await _dbContext.CityPopulationDeletionStates.AsNoTracking().AnyAsync(x => x.CityId == cityId, cancellationToken);
        }

        private async Task<DateOnly> ResolveCurrentDateAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            DateOnly? projectionDate = await _dbContext.CityPopulationSummaryProjections
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => (DateOnly?)x.CurrentDate)
               .SingleOrDefaultAsync(cancellationToken);

            if (projectionDate.HasValue)
                return projectionDate.Value;

            DateOnly? progressionDate = await _dbContext.CityPopulationProgressionStates
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => (DateOnly?)x.LastProcessedDate)
               .SingleOrDefaultAsync(cancellationToken);

            if (progressionDate.HasValue)
                return progressionDate.Value;

            DateTimeOffset? weatherExposureTimestamp = await _dbContext.CityPopulationWeatherExposureStates
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => (DateTimeOffset?)x.LastExposureProcessedAtSimTimeUtc)
               .SingleOrDefaultAsync(cancellationToken);

            if (weatherExposureTimestamp.HasValue)
                return DateOnly.FromDateTime(weatherExposureTimestamp.Value.UtcDateTime);

            DateTimeOffset? weatherImpactTimestamp = await _dbContext.CityPopulationWeatherImpactStates
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => (DateTimeOffset?)x.LastAppliedAtSimTimeUtc)
               .SingleOrDefaultAsync(cancellationToken);

            if (weatherImpactTimestamp.HasValue)
                return DateOnly.FromDateTime(weatherImpactTimestamp.Value.UtcDateTime);

            DateTimeOffset? archivedAtUtc = await _dbContext.CityPopulationArchiveStates
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => (DateTimeOffset?)x.ArchivedAtUtc)
               .SingleOrDefaultAsync(cancellationToken);

            if (archivedAtUtc.HasValue)
                return DateOnly.FromDateTime(archivedAtUtc.Value.UtcDateTime);

            DateTimeOffset? deletedAtUtc = await _dbContext.CityPopulationDeletionStates
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => (DateTimeOffset?)x.DeletedAtUtc)
               .SingleOrDefaultAsync(cancellationToken);

            return deletedAtUtc.HasValue
                ? DateOnly.FromDateTime(deletedAtUtc.Value.UtcDateTime)
                : DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}

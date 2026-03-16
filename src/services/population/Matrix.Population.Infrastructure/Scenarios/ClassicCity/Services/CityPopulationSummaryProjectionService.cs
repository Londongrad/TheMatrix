using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
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

            CityPopulationDailySummarySnapshot[] snapshots = await _dbContext.CityPopulationDailySummarySnapshots
               .Where(x => x.CityId == cityId)
               .ToArrayAsync(cancellationToken);

            if (snapshots.Length > 0)
                _dbContext.CityPopulationDailySummarySnapshots.RemoveRange(snapshots);
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

            CityPopulationSummarySnapshotValues snapshotValues = BuildSnapshotValues(
                currentDate: currentDate,
                persons: persons,
                householdPlacements: resolvedPlacements);

            await UpsertSummaryProjectionAsync(
                cityId: cityId,
                snapshot: snapshotValues,
                cancellationToken: cancellationToken);

            await UpsertDailySnapshotAsync(
                cityId: cityId,
                snapshot: snapshotValues,
                cancellationToken: cancellationToken);
        }

        private static CityPopulationSummarySnapshotValues BuildSnapshotValues(
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements)
        {
            DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
            Dictionary<HouseholdId, HousingStatus> housingByHouseholdId = householdPlacements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.HousingStatus);

            Person[] aliveResidents = persons
               .Where(x => x.IsAlive)
               .ToArray();

            return new CityPopulationSummarySnapshotValues(
                currentDate,
                updatedAtUtc,
                householdPlacements.Count,
                householdPlacements.Count(x => x.HousingStatus == HousingStatus.Housed),
                householdPlacements.Count(x => x.HousingStatus == HousingStatus.Homeless),
                aliveResidents.Length,
                persons.Count - aliveResidents.Length,
                aliveResidents.Count(x =>
                    housingByHouseholdId.TryGetValue(x.HouseholdId, out HousingStatus status) &&
                    status == HousingStatus.Housed),
                aliveResidents.Count(x =>
                    housingByHouseholdId.TryGetValue(x.HouseholdId, out HousingStatus status) &&
                    status == HousingStatus.Homeless),
                aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Child),
                aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Youth),
                aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Adult),
                aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Senior),
                aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Employed),
                aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Student),
                aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Unemployed),
                aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Retired),
                aliveResidents.Select(x => (decimal?)x.Health.Value).Average(),
                aliveResidents.Select(x => (decimal?)x.Happiness.Value).Average(),
                aliveResidents.Select(x => (decimal?)x.Energy.Value).Average(),
                aliveResidents.Select(x => (decimal?)x.Stress.Value).Average(),
                aliveResidents.Select(x => (decimal?)x.SocialNeed.Value).Average());
        }

        private Task UpsertSummaryProjectionAsync(
            CityId cityId,
            CityPopulationSummarySnapshotValues snapshot,
            CancellationToken cancellationToken)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO "CityPopulationSummaryProjections"
                 ("CityId", "AdultCount", "AverageEnergy", "AverageHappiness", "AverageHealth", "AverageSocialNeed", "AverageStress", "ChildCount", "CurrentDate", "DeceasedCount", "EmployedCount", "HomelessHouseholdCount", "HomelessResidentCount", "HousedHouseholdCount", "HousedResidentCount", "HouseholdCount", "ResidentCount", "RetiredCount", "SeniorCount", "StudentCount", "UnemployedCount", "UpdatedAtUtc", "YouthCount")
                 VALUES
                 ({cityId.Value}, {snapshot.AdultCount}, {snapshot.AverageEnergy}, {snapshot.AverageHappiness}, {snapshot.AverageHealth}, {snapshot.AverageSocialNeed}, {snapshot.AverageStress}, {snapshot.ChildCount}, {snapshot.CurrentDate}, {snapshot.DeceasedCount}, {snapshot.EmployedCount}, {snapshot.HomelessHouseholdCount}, {snapshot.HomelessResidentCount}, {snapshot.HousedHouseholdCount}, {snapshot.HousedResidentCount}, {snapshot.HouseholdCount}, {snapshot.ResidentCount}, {snapshot.RetiredCount}, {snapshot.SeniorCount}, {snapshot.StudentCount}, {snapshot.UnemployedCount}, {snapshot.UpdatedAtUtc}, {snapshot.YouthCount})
                 ON CONFLICT ("CityId") DO UPDATE SET
                     "AdultCount" = EXCLUDED."AdultCount",
                     "AverageEnergy" = EXCLUDED."AverageEnergy",
                     "AverageHappiness" = EXCLUDED."AverageHappiness",
                     "AverageHealth" = EXCLUDED."AverageHealth",
                     "AverageSocialNeed" = EXCLUDED."AverageSocialNeed",
                     "AverageStress" = EXCLUDED."AverageStress",
                     "ChildCount" = EXCLUDED."ChildCount",
                     "CurrentDate" = EXCLUDED."CurrentDate",
                     "DeceasedCount" = EXCLUDED."DeceasedCount",
                     "EmployedCount" = EXCLUDED."EmployedCount",
                     "HomelessHouseholdCount" = EXCLUDED."HomelessHouseholdCount",
                     "HomelessResidentCount" = EXCLUDED."HomelessResidentCount",
                     "HousedHouseholdCount" = EXCLUDED."HousedHouseholdCount",
                     "HousedResidentCount" = EXCLUDED."HousedResidentCount",
                     "HouseholdCount" = EXCLUDED."HouseholdCount",
                     "ResidentCount" = EXCLUDED."ResidentCount",
                     "RetiredCount" = EXCLUDED."RetiredCount",
                     "SeniorCount" = EXCLUDED."SeniorCount",
                     "StudentCount" = EXCLUDED."StudentCount",
                     "UnemployedCount" = EXCLUDED."UnemployedCount",
                     "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc",
                     "YouthCount" = EXCLUDED."YouthCount";
                 """,
                cancellationToken);
        }

        private Task UpsertDailySnapshotAsync(
            CityId cityId,
            CityPopulationSummarySnapshotValues snapshot,
            CancellationToken cancellationToken)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO "CityPopulationDailySummarySnapshots"
                 ("CityId", "SnapshotDate", "AdultCount", "AverageEnergy", "AverageHappiness", "AverageHealth", "AverageSocialNeed", "AverageStress", "ChildCount", "DeceasedCount", "EmployedCount", "HomelessHouseholdCount", "HomelessResidentCount", "HousedHouseholdCount", "HousedResidentCount", "HouseholdCount", "ResidentCount", "RetiredCount", "SeniorCount", "StudentCount", "UnemployedCount", "UpdatedAtUtc", "YouthCount")
                 VALUES
                 ({cityId.Value}, {snapshot.CurrentDate}, {snapshot.AdultCount}, {snapshot.AverageEnergy}, {snapshot.AverageHappiness}, {snapshot.AverageHealth}, {snapshot.AverageSocialNeed}, {snapshot.AverageStress}, {snapshot.ChildCount}, {snapshot.DeceasedCount}, {snapshot.EmployedCount}, {snapshot.HomelessHouseholdCount}, {snapshot.HomelessResidentCount}, {snapshot.HousedHouseholdCount}, {snapshot.HousedResidentCount}, {snapshot.HouseholdCount}, {snapshot.ResidentCount}, {snapshot.RetiredCount}, {snapshot.SeniorCount}, {snapshot.StudentCount}, {snapshot.UnemployedCount}, {snapshot.UpdatedAtUtc}, {snapshot.YouthCount})
                 ON CONFLICT ("CityId", "SnapshotDate") DO UPDATE SET
                     "AdultCount" = EXCLUDED."AdultCount",
                     "AverageEnergy" = EXCLUDED."AverageEnergy",
                     "AverageHappiness" = EXCLUDED."AverageHappiness",
                     "AverageHealth" = EXCLUDED."AverageHealth",
                     "AverageSocialNeed" = EXCLUDED."AverageSocialNeed",
                     "AverageStress" = EXCLUDED."AverageStress",
                     "ChildCount" = EXCLUDED."ChildCount",
                     "DeceasedCount" = EXCLUDED."DeceasedCount",
                     "EmployedCount" = EXCLUDED."EmployedCount",
                     "HomelessHouseholdCount" = EXCLUDED."HomelessHouseholdCount",
                     "HomelessResidentCount" = EXCLUDED."HomelessResidentCount",
                     "HousedHouseholdCount" = EXCLUDED."HousedHouseholdCount",
                     "HousedResidentCount" = EXCLUDED."HousedResidentCount",
                     "HouseholdCount" = EXCLUDED."HouseholdCount",
                     "ResidentCount" = EXCLUDED."ResidentCount",
                     "RetiredCount" = EXCLUDED."RetiredCount",
                     "SeniorCount" = EXCLUDED."SeniorCount",
                     "StudentCount" = EXCLUDED."StudentCount",
                     "UnemployedCount" = EXCLUDED."UnemployedCount",
                     "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc",
                     "YouthCount" = EXCLUDED."YouthCount";
                 """,
                cancellationToken);
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

        private sealed record CityPopulationSummarySnapshotValues(
            DateOnly CurrentDate,
            DateTimeOffset UpdatedAtUtc,
            int HouseholdCount,
            int HousedHouseholdCount,
            int HomelessHouseholdCount,
            int ResidentCount,
            int DeceasedCount,
            int HousedResidentCount,
            int HomelessResidentCount,
            int ChildCount,
            int YouthCount,
            int AdultCount,
            int SeniorCount,
            int EmployedCount,
            int StudentCount,
            int UnemployedCount,
            int RetiredCount,
            decimal? AverageHealth,
            decimal? AverageHappiness,
            decimal? AverageEnergy,
            decimal? AverageStress,
            decimal? AverageSocialNeed);
    }
}

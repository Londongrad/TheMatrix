using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationDashboardReadRepository(PopulationDbContext dbContext)
        : ICityPopulationDashboardReadRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<CityPopulationDashboardSnapshotReadModel?> GetCurrentSnapshotAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            CityPopulationSummaryProjection? projection = await _dbContext
               .CityPopulationSummaryProjections
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            return projection is null
                ? null
                : MapSnapshot(
                    cityId: projection.CityId.Value,
                    snapshotDate: projection.CurrentDate,
                    householdCount: projection.HouseholdCount,
                    housedHouseholdCount: projection.HousedHouseholdCount,
                    homelessHouseholdCount: projection.HomelessHouseholdCount,
                    residentCount: projection.ResidentCount,
                    deceasedCount: projection.DeceasedCount,
                    housedResidentCount: projection.HousedResidentCount,
                    homelessResidentCount: projection.HomelessResidentCount,
                    childCount: projection.ChildCount,
                    youthCount: projection.YouthCount,
                    adultCount: projection.AdultCount,
                    seniorCount: projection.SeniorCount,
                    employedCount: projection.EmployedCount,
                    studentCount: projection.StudentCount,
                    unemployedCount: projection.UnemployedCount,
                    retiredCount: projection.RetiredCount,
                    averageHealth: projection.AverageHealth,
                    averageHappiness: projection.AverageHappiness,
                    averageEnergy: projection.AverageEnergy,
                    averageStress: projection.AverageStress,
                    averageSocialNeed: projection.AverageSocialNeed);
        }

        public async Task<CityPopulationDashboardSnapshotReadModel?> GetSnapshotOnOrBeforeAsync(
            CityId cityId,
            DateOnly snapshotDate,
            CancellationToken cancellationToken = default)
        {
            CityPopulationDailySummarySnapshot? snapshot = await _dbContext
               .CityPopulationDailySummarySnapshots
               .AsNoTracking()
               .Where(x => x.CityId == cityId && x.SnapshotDate <= snapshotDate)
               .OrderByDescending(x => x.SnapshotDate)
               .FirstOrDefaultAsync(cancellationToken);

            return snapshot is null
                ? null
                : MapSnapshot(
                    cityId: snapshot.CityId.Value,
                    snapshotDate: snapshot.SnapshotDate,
                    householdCount: snapshot.HouseholdCount,
                    housedHouseholdCount: snapshot.HousedHouseholdCount,
                    homelessHouseholdCount: snapshot.HomelessHouseholdCount,
                    residentCount: snapshot.ResidentCount,
                    deceasedCount: snapshot.DeceasedCount,
                    housedResidentCount: snapshot.HousedResidentCount,
                    homelessResidentCount: snapshot.HomelessResidentCount,
                    childCount: snapshot.ChildCount,
                    youthCount: snapshot.YouthCount,
                    adultCount: snapshot.AdultCount,
                    seniorCount: snapshot.SeniorCount,
                    employedCount: snapshot.EmployedCount,
                    studentCount: snapshot.StudentCount,
                    unemployedCount: snapshot.UnemployedCount,
                    retiredCount: snapshot.RetiredCount,
                    averageHealth: snapshot.AverageHealth,
                    averageHappiness: snapshot.AverageHappiness,
                    averageEnergy: snapshot.AverageEnergy,
                    averageStress: snapshot.AverageStress,
                    averageSocialNeed: snapshot.AverageSocialNeed);
        }

        public async Task<IReadOnlyList<CityPopulationActivityEventReadModel>> ListRecentActivityAsync(
            CityId cityId,
            int take,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityPopulationActivityEvents
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .OrderByDescending(x => x.OccurredAtUtc)
               .Take(take)
               .Select(x => new CityPopulationActivityEventReadModel(
                    x.ActivityEventId,
                    x.CurrentDate,
                    x.OccurredAtUtc,
                    x.EventType.ToString(),
                    x.Source.ToString(),
                    x.Severity.ToString(),
                    x.Title,
                    x.Summary,
                    x.PrimaryResidentId == null ? null : x.PrimaryResidentId.Value.Value,
                    x.SecondaryResidentId == null ? null : x.SecondaryResidentId.Value.Value))
               .ToArrayAsync(cancellationToken);
        }

        private static CityPopulationDashboardSnapshotReadModel MapSnapshot(
            Guid cityId,
            DateOnly snapshotDate,
            int householdCount,
            int housedHouseholdCount,
            int homelessHouseholdCount,
            int residentCount,
            int deceasedCount,
            int housedResidentCount,
            int homelessResidentCount,
            int childCount,
            int youthCount,
            int adultCount,
            int seniorCount,
            int employedCount,
            int studentCount,
            int unemployedCount,
            int retiredCount,
            decimal? averageHealth,
            decimal? averageHappiness,
            decimal? averageEnergy,
            decimal? averageStress,
            decimal? averageSocialNeed)
        {
            return new CityPopulationDashboardSnapshotReadModel(
                CityId: cityId,
                SnapshotDate: snapshotDate,
                HouseholdCount: householdCount,
                HousedHouseholdCount: housedHouseholdCount,
                HomelessHouseholdCount: homelessHouseholdCount,
                ResidentCount: residentCount,
                DeceasedCount: deceasedCount,
                HousedResidentCount: housedResidentCount,
                HomelessResidentCount: homelessResidentCount,
                ChildCount: childCount,
                YouthCount: youthCount,
                AdultCount: adultCount,
                SeniorCount: seniorCount,
                EmployedCount: employedCount,
                StudentCount: studentCount,
                UnemployedCount: unemployedCount,
                RetiredCount: retiredCount,
                AverageHealth: averageHealth,
                AverageHappiness: averageHappiness,
                AverageEnergy: averageEnergy,
                AverageStress: averageStress,
                AverageSocialNeed: averageSocialNeed);
        }
    }
}

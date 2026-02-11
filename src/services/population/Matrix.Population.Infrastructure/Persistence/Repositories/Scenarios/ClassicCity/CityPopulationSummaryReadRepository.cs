using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationSummaryReadRepository(PopulationDbContext dbContext)
        : ICityPopulationSummaryReadRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<CityPopulationSummaryReadModel?> GetByCityIdAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default)
        {
            HouseholdAggregate householdAggregate = await GetHouseholdAggregateAsync(
                                                        cityId: cityId,
                                                        cancellationToken: cancellationToken) ??
                                                    HouseholdAggregate.Empty;

            ResidentAggregate residentAggregate = await GetResidentAggregateAsync(
                                                      cityId: cityId,
                                                      currentDate: currentDate,
                                                      cancellationToken: cancellationToken) ??
                                                  ResidentAggregate.Empty;

            CityPopulationEnvironment? environment = await _dbContext
               .CityPopulationEnvironments
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            CityPopulationProgressionState? progression = await _dbContext
               .CityPopulationProgressionStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            CityPopulationWeatherExposureState? weatherExposure = await _dbContext
               .CityPopulationWeatherExposureStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            CityPopulationWeatherImpactState? weatherImpact = await _dbContext
               .CityPopulationWeatherImpactStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            CityPopulationArchiveState? archiveState = await _dbContext
               .CityPopulationArchiveStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            CityPopulationDeletionState? deletionState = await _dbContext
               .CityPopulationDeletionStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            bool exists = householdAggregate.HouseholdCount > 0 ||
                          residentAggregate.ResidentCount > 0 ||
                          residentAggregate.DeceasedCount > 0 ||
                          environment is not null ||
                          progression is not null ||
                          weatherExposure is not null ||
                          weatherImpact is not null ||
                          archiveState is not null ||
                          deletionState is not null;

            if (!exists)
                return null;

            return new CityPopulationSummaryReadModel(
                CityId: cityId.Value,
                CurrentDate: currentDate,
                IsArchived: archiveState is not null,
                ArchivedAtUtc: archiveState?.ArchivedAtUtc,
                IsDeleted: deletionState is not null,
                DeletedAtUtc: deletionState?.DeletedAtUtc,
                ClimateZone: environment?.ClimateZone,
                Hemisphere: environment?.Hemisphere,
                UtcOffsetMinutes: environment?.UtcOffsetMinutes,
                EnvironmentUpdatedAtUtc: environment?.UpdatedAtUtc,
                LastProcessedTickId: progression?.LastProcessedTickId,
                LastProcessedDate: progression?.LastProcessedDate,
                SimulationUpdatedAtUtc: progression?.UpdatedAtUtc,
                CurrentWeatherType: weatherExposure?.CurrentType,
                CurrentWeatherSeverity: weatherExposure?.CurrentSeverity,
                IsWeatherRecoveryActive: weatherExposure?.HasRecoverySource ?? false,
                CurrentWeatherEffectiveAtSimTimeUtc: weatherExposure?.CurrentWeatherEffectiveAtSimTimeUtc,
                LastWeatherOccurredOnUtc: weatherExposure?.LastWeatherOccurredOnUtc,
                LastExposureProcessedAtSimTimeUtc: weatherExposure?.LastExposureProcessedAtSimTimeUtc,
                LastWeatherImpactAppliedAtSimTimeUtc: weatherImpact?.LastAppliedAtSimTimeUtc,
                HouseholdCount: householdAggregate.HouseholdCount,
                HousedHouseholdCount: householdAggregate.HousedHouseholdCount,
                HomelessHouseholdCount: householdAggregate.HomelessHouseholdCount,
                ResidentCount: residentAggregate.ResidentCount,
                DeceasedCount: residentAggregate.DeceasedCount,
                HousedResidentCount: residentAggregate.HousedResidentCount,
                HomelessResidentCount: residentAggregate.HomelessResidentCount,
                ChildCount: residentAggregate.ChildCount,
                YouthCount: residentAggregate.YouthCount,
                AdultCount: residentAggregate.AdultCount,
                SeniorCount: residentAggregate.SeniorCount,
                EmployedCount: residentAggregate.EmployedCount,
                StudentCount: residentAggregate.StudentCount,
                UnemployedCount: residentAggregate.UnemployedCount,
                RetiredCount: residentAggregate.RetiredCount,
                AverageHealth: residentAggregate.AverageHealth,
                AverageHappiness: residentAggregate.AverageHappiness,
                AverageEnergy: residentAggregate.AverageEnergy,
                AverageStress: residentAggregate.AverageStress,
                AverageSocialNeed: residentAggregate.AverageSocialNeed);
        }

        private async Task<HouseholdAggregate?> GetHouseholdAggregateAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await _dbContext
               .ClassicCityHouseholdPlacements
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .GroupBy(_ => 1)
               .Select(g => new HouseholdAggregate(
                    g.Count(),
                    g.Count(x => x.HousingStatus == HousingStatus.Housed),
                    g.Count(x => x.HousingStatus == HousingStatus.Homeless)))
               .SingleOrDefaultAsync(cancellationToken);
        }

        private async Task<ResidentAggregate?> GetResidentAggregateAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken)
        {
            string aliveStatus = LifeStatus.Alive.ToString();
            string deceasedStatus = LifeStatus.Deceased.ToString();
            string employedStatus = EmploymentStatus.Employed.ToString();
            string studentStatus = EmploymentStatus.Student.ToString();
            string unemployedStatus = EmploymentStatus.Unemployed.ToString();
            string retiredStatus = EmploymentStatus.Retired.ToString();

            var childBoundary = currentDate.AddYears(-7)
               .ToDateTime(TimeOnly.MinValue);
            var youthBoundary = currentDate.AddYears(-17)
               .ToDateTime(TimeOnly.MinValue);
            var seniorBoundary = currentDate.AddYears(-66)
               .ToDateTime(TimeOnly.MinValue);

            var residentsQuery =
                from person in _dbContext.Persons.AsNoTracking()
                join householdPlacement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                       .Where(x => x.CityId == cityId)
                    on person.HouseholdId equals householdPlacement.HouseholdId
                select new
                {
                    LifeStatus = EF.Property<string>(
                        person,
                        "LifeStatus"),
                    EmploymentStatus = EF.Property<string>(
                        person,
                        "EmploymentStatus"),
                    BirthDate = EF.Property<DateTime>(
                        person,
                        "BirthDate"),
                    Health = EF.Property<int>(
                        person,
                        "Health"),
                    Happiness = EF.Property<int>(
                        person,
                        "Happiness"),
                    Energy = EF.Property<int>(
                        person,
                        "Energy"),
                    Stress = EF.Property<int>(
                        person,
                        "Stress"),
                    SocialNeed = EF.Property<int>(
                        person,
                        "SocialNeed"),
                    householdPlacement.HousingStatus
                };

            return await residentsQuery
               .GroupBy(_ => 1)
               .Select(g => new ResidentAggregate(
                    g.Count(x => x.LifeStatus == aliveStatus),
                    g.Count(x => x.LifeStatus == deceasedStatus),
                    g.Count(x => x.LifeStatus == aliveStatus && x.HousingStatus == HousingStatus.Housed),
                    g.Count(x => x.LifeStatus == aliveStatus && x.HousingStatus == HousingStatus.Homeless),
                    g.Count(x => x.LifeStatus == aliveStatus && x.BirthDate > childBoundary),
                    g.Count(x => x.LifeStatus == aliveStatus &&
                                 x.BirthDate <= childBoundary &&
                                 x.BirthDate > youthBoundary),
                    g.Count(x => x.LifeStatus == aliveStatus &&
                                 x.BirthDate <= youthBoundary &&
                                 x.BirthDate > seniorBoundary),
                    g.Count(x => x.LifeStatus == aliveStatus && x.BirthDate <= seniorBoundary),
                    g.Count(x => x.LifeStatus == aliveStatus && x.EmploymentStatus == employedStatus),
                    g.Count(x => x.LifeStatus == aliveStatus && x.EmploymentStatus == studentStatus),
                    g.Count(x => x.LifeStatus == aliveStatus && x.EmploymentStatus == unemployedStatus),
                    g.Count(x => x.LifeStatus == aliveStatus && x.EmploymentStatus == retiredStatus),
                    g.Where(x => x.LifeStatus == aliveStatus)
                       .Select(x => (decimal?)x.Health)
                       .Average(),
                    g.Where(x => x.LifeStatus == aliveStatus)
                       .Select(x => (decimal?)x.Happiness)
                       .Average(),
                    g.Where(x => x.LifeStatus == aliveStatus)
                       .Select(x => (decimal?)x.Energy)
                       .Average(),
                    g.Where(x => x.LifeStatus == aliveStatus)
                       .Select(x => (decimal?)x.Stress)
                       .Average(),
                    g.Where(x => x.LifeStatus == aliveStatus)
                       .Select(x => (decimal?)x.SocialNeed)
                       .Average()))
               .SingleOrDefaultAsync(cancellationToken);
        }

        private sealed record HouseholdAggregate(
            int HouseholdCount,
            int HousedHouseholdCount,
            int HomelessHouseholdCount)
        {
            public static HouseholdAggregate Empty { get; } = new(
                HouseholdCount: 0,
                HousedHouseholdCount: 0,
                HomelessHouseholdCount: 0);
        }

        private sealed record ResidentAggregate(
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
            decimal? AverageSocialNeed)
        {
            public static ResidentAggregate Empty { get; } = new(
                ResidentCount: 0,
                DeceasedCount: 0,
                HousedResidentCount: 0,
                HomelessResidentCount: 0,
                ChildCount: 0,
                YouthCount: 0,
                AdultCount: 0,
                SeniorCount: 0,
                EmployedCount: 0,
                StudentCount: 0,
                UnemployedCount: 0,
                RetiredCount: 0,
                AverageHealth: null,
                AverageHappiness: null,
                AverageEnergy: null,
                AverageStress: null,
                AverageSocialNeed: null);
        }
    }
}

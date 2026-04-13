using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
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
            CancellationToken cancellationToken = default)
        {
            CityPopulationSummaryProjection? projection = await _dbContext
               .CityPopulationSummaryProjections
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

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

            bool exists = projection is not null ||
                          environment is not null ||
                          progression is not null ||
                          weatherExposure is not null ||
                          weatherImpact is not null ||
                          archiveState is not null ||
                          deletionState is not null;

            if (!exists)
                return null;

            DateOnly currentDate = projection?.CurrentDate ??
                                   progression?.LastProcessedDate ??
                                   ResolveCurrentDateFallback(
                                       weatherExposure: weatherExposure,
                                       weatherImpact: weatherImpact,
                                       archiveState: archiveState,
                                       deletionState: deletionState);

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
                HouseholdCount: projection?.HouseholdCount ?? 0,
                HousedHouseholdCount: projection?.HousedHouseholdCount ?? 0,
                HomelessHouseholdCount: projection?.HomelessHouseholdCount ?? 0,
                ResidentCount: projection?.ResidentCount ?? 0,
                DeceasedCount: projection?.DeceasedCount ?? 0,
                HousedResidentCount: projection?.HousedResidentCount ?? 0,
                HomelessResidentCount: projection?.HomelessResidentCount ?? 0,
                ChildCount: projection?.ChildCount ?? 0,
                YouthCount: projection?.YouthCount ?? 0,
                AdultCount: projection?.AdultCount ?? 0,
                SeniorCount: projection?.SeniorCount ?? 0,
                EmployedCount: projection?.EmployedCount ?? 0,
                StudentCount: projection?.StudentCount ?? 0,
                UnemployedCount: projection?.UnemployedCount ?? 0,
                RetiredCount: projection?.RetiredCount ?? 0,
                AverageHealth: projection?.AverageHealth,
                AverageHappiness: projection?.AverageHappiness,
                AverageEnergy: projection?.AverageEnergy,
                AverageStress: projection?.AverageStress,
                AverageSocialNeed: projection?.AverageSocialNeed,
                ActiveIllnessCount: projection?.ActiveIllnessCount ?? 0,
                SevereIllnessCount: projection?.SevereIllnessCount ?? 0,
                MedicalLoadIndex: projection?.MedicalLoadIndex,
                TriagePressureIndex: projection?.TriagePressureIndex,
                RecoverySupportIndex: projection?.RecoverySupportIndex,
                WorkforceCommuteAccessibilityIndex: projection?.WorkforceCommuteAccessibilityIndex,
                WorkforceAttendanceIndex: projection?.WorkforceAttendanceIndex,
                WorkforceProductivityIndex: projection?.WorkforceProductivityIndex,
                StudentCommuteAccessibilityIndex: projection?.StudentCommuteAccessibilityIndex,
                StudentAttendanceIndex: projection?.StudentAttendanceIndex);
        }

        private static DateOnly ResolveCurrentDateFallback(
            CityPopulationWeatherExposureState? weatherExposure,
            CityPopulationWeatherImpactState? weatherImpact,
            CityPopulationArchiveState? archiveState,
            CityPopulationDeletionState? deletionState)
        {
            DateTimeOffset? fallbackTimestamp = weatherExposure?.LastExposureProcessedAtSimTimeUtc ??
                                                weatherImpact?.LastAppliedAtSimTimeUtc ??
                                                archiveState?.ArchivedAtUtc ??
                                                deletionState?.DeletedAtUtc;

            return fallbackTimestamp.HasValue
                ? DateOnly.FromDateTime(fallbackTimestamp.Value.UtcDateTime)
                : DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}

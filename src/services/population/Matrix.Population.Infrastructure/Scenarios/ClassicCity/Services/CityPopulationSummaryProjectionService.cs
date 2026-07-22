using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationSummaryProjectionService(
        PopulationDbContext dbContext,
        CityPopulationDistrictImpactPolicy districtImpactPolicy,
        CityPopulationParticipationPolicy participationPolicy,
        TimeProvider timeProvider,
        ICityPopulationCommuteRoutingService commuteRoutingService,
        IEducationParticipationProjectionRepository educationParticipationProjectionRepository,
        ILogger<CityPopulationSummaryProjectionService> logger)
        : ICityPopulationSummaryProjectionService
    {
        private readonly ICityPopulationCommuteRoutingService _commuteRoutingService = commuteRoutingService;
        private readonly PopulationDbContext _dbContext = dbContext;
        private readonly CityPopulationDistrictImpactPolicy _districtImpactPolicy = districtImpactPolicy;
        private readonly IEducationParticipationProjectionRepository _educationParticipationProjectionRepository =
            educationParticipationProjectionRepository;
        private readonly ILogger<CityPopulationSummaryProjectionService> _logger = logger;
        private readonly CityPopulationParticipationPolicy _participationPolicy = participationPolicy;
        private readonly TimeProvider _timeProvider = timeProvider;

        public Task UpdateAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            bool includeCommuteMetrics = true,
            CancellationToken cancellationToken = default)
        {
            return UpsertAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: null,
                includeCommuteMetrics: includeCommuteMetrics,
                cancellationToken: cancellationToken);
        }

        public Task UpdateAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            bool includeCommuteMetrics = true,
            CancellationToken cancellationToken = default)
        {
            return UpsertAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: householdPlacements,
                includeCommuteMetrics: includeCommuteMetrics,
                cancellationToken: cancellationToken);
        }

        public async Task RebuildAsync(
            CityId cityId,
            DateOnly currentDate,
            bool includeCommuteMetrics = true,
            CancellationToken cancellationToken = default)
        {
            List<Person> persons = await _dbContext.Persons
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .ToListAsync(cancellationToken);

            await UpsertAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: persons,
                householdPlacements: null,
                includeCommuteMetrics: includeCommuteMetrics,
                cancellationToken: cancellationToken);
        }

        public async Task EnsureExistsAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            var projectionPresence = await _dbContext.CityPopulationSummaryProjections
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => new
               {
                   x.ResidentCount,
                   x.HouseholdCount
               })
               .SingleOrDefaultAsync(cancellationToken);

            if (projectionPresence is not null)
            {
                if (projectionPresence.ResidentCount > 0 || projectionPresence.HouseholdCount > 0)
                    return;

                (int actualResidentCount, int actualHouseholdCount) = await GetPersistedPopulationCountsAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

                if (actualResidentCount == 0 && actualHouseholdCount == 0)
                    return;

                _logger.LogWarning(
                    message:
                    "Repairing zeroed city population summary projection for cityId={CityId}. Actual residents={ResidentCount}, households={HouseholdCount}.",
                    cityId.Value,
                    actualResidentCount,
                    actualHouseholdCount);

                await RebuildAsync(
                    cityId: cityId,
                    currentDate: await ResolveCurrentDateAsync(
                        cityId: cityId,
                        cancellationToken: cancellationToken),
                    includeCommuteMetrics: true,
                    cancellationToken: cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

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
                includeCommuteMetrics: true,
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
            bool includeCommuteMetrics,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ClassicCityHouseholdPlacement> resolvedPlacements =
                await ResolvePlacementsAsync(
                    cityId: cityId,
                    householdPlacements: householdPlacements,
                    cancellationToken: cancellationToken);
            IReadOnlyCollection<Person> resolvedPersons = await ResolvePersonsAsync(
                cityId: cityId,
                persons: persons,
                householdPlacements: resolvedPlacements,
                cancellationToken: cancellationToken);
            IReadOnlyDictionary<Guid, EducationParticipationProjection> educationParticipations =
                resolvedPersons.Count == 0
                    ? new Dictionary<Guid, EducationParticipationProjection>()
                    : await _educationParticipationProjectionRepository.GetEnrolledByResidentIdsAsync(
                        simulationHostId: cityId.Value,
                        residentIds: resolvedPersons.Select(person => person.Id.Value).ToArray(),
                        cancellationToken: cancellationToken);
            var educationParticipationIndex = new EducationParticipationProjectionIndex(
                cityId.Value,
                educationParticipations);

            if ((persons.Count == 0 || householdPlacements?.Count == 0) &&
                (resolvedPersons.Count > 0 || resolvedPlacements.Count > 0))
                _logger.LogWarning(
                    message:
                    "Recovered missing city population summary inputs for cityId={CityId}. Incoming residents={IncomingResidentCount}, incoming households={IncomingHouseholdCount}, resolved residents={ResolvedResidentCount}, resolved households={ResolvedHouseholdCount}.",
                    cityId.Value,
                    persons.Count,
                    householdPlacements?.Count ?? 0,
                    resolvedPersons.Count,
                    resolvedPlacements.Count);

            CityPopulationLivingConditionsState? livingConditionsState = await _dbContext
               .CityPopulationLivingConditionsStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);
            CityPopulationEssentialsState? essentialsState = await _dbContext.CityPopulationEssentialsStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);
            CityHealthcarePressureSnapshotEntity? healthcarePressureSnapshot =
                await _dbContext.CityHealthcarePressureSnapshots
                   .AsNoTracking()
                   .SingleOrDefaultAsync(
                        snapshot => snapshot.CityId == cityId.Value,
                        cancellationToken);
            CityPopulationHealthcarePressureProfile healthcarePressureProfile =
                healthcarePressureSnapshot is null
                    ? CityPopulationHealthcarePressureProfile.Baseline
                    : new CityPopulationHealthcarePressureProfile(
                        healthcarePressureSnapshot.ActiveIllnessCount,
                        healthcarePressureSnapshot.SevereIllnessCount,
                        healthcarePressureSnapshot.MedicalLoadIndex,
                        healthcarePressureSnapshot.TriagePressureIndex,
                        healthcarePressureSnapshot.RecoverySupportIndex);

            CityPopulationSummarySnapshotValues snapshotValues = await BuildSnapshotValuesAsync(
                cityId: cityId,
                currentDate: currentDate,
                persons: resolvedPersons,
                educationParticipationIndex: educationParticipationIndex,
                householdPlacements: resolvedPlacements,
                livingConditionsState: livingConditionsState,
                essentialsState: essentialsState,
                districtImpactPolicy: _districtImpactPolicy,
                participationPolicy: _participationPolicy,
                healthcarePressureProfile: healthcarePressureProfile,
                timeProvider: _timeProvider,
                commuteRoutingService: _commuteRoutingService,
                includeCommuteMetrics: includeCommuteMetrics,
                cancellationToken: cancellationToken);

            await UpsertSummaryProjectionAsync(
                cityId: cityId,
                snapshot: snapshotValues,
                cancellationToken: cancellationToken);

            await UpsertDailySnapshotAsync(
                cityId: cityId,
                snapshot: snapshotValues,
                cancellationToken: cancellationToken);
        }

        private async Task<IReadOnlyCollection<ClassicCityHouseholdPlacement>> ResolvePlacementsAsync(
            CityId cityId,
            IReadOnlyCollection<ClassicCityHouseholdPlacement>? householdPlacements,
            CancellationToken cancellationToken)
        {
            if (householdPlacements is
                {
                    Count: > 0
                })
                return householdPlacements;

            ClassicCityHouseholdPlacement[] trackedPlacements = _dbContext.ChangeTracker
               .Entries<ClassicCityHouseholdPlacement>()
               .Where(x => x.State != EntityState.Deleted && x.Entity.CityId == cityId)
               .Select(x => x.Entity)
               .DistinctBy(x => x.HouseholdId)
               .ToArray();

            if (trackedPlacements.Length > 0)
                return trackedPlacements;

            return await _dbContext.ClassicCityHouseholdPlacements
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyCollection<Person>> ResolvePersonsAsync(
            CityId cityId,
            IReadOnlyCollection<Person> persons,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken)
        {
            if (persons.Count > 0)
                return persons;

            if (householdPlacements.Count == 0)
                return persons;

            var householdIds = householdPlacements
               .Select(x => x.HouseholdId)
               .ToHashSet();
            Person[] trackedPersons = _dbContext.ChangeTracker
               .Entries<Person>()
               .Where(x => x.State != EntityState.Deleted && householdIds.Contains(x.Entity.HouseholdId))
               .Select(x => x.Entity)
               .DistinctBy(x => x.Id)
               .ToArray();

            if (trackedPersons.Length > 0)
                return trackedPersons;

            return await _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements
                       .AsNoTracking()
                       .Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .ToListAsync(cancellationToken);
        }

        private static async Task<CityPopulationSummarySnapshotValues> BuildSnapshotValuesAsync(
            CityId cityId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> persons,
            EducationParticipationProjectionIndex educationParticipationIndex,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CityPopulationLivingConditionsState? livingConditionsState,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationParticipationPolicy participationPolicy,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile,
            TimeProvider timeProvider,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            bool includeCommuteMetrics,
            CancellationToken cancellationToken)
        {
            DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();
            var housingByHouseholdId = householdPlacements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.HousingStatus);
            var districtByHouseholdId = householdPlacements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.DistrictId);
            var residentialBuildingByHouseholdId = householdPlacements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.ResidentialBuildingId);

            Person[] aliveResidents = persons
               .Where(x => x.IsAlive)
               .ToArray();
            Person[] employedResidents = aliveResidents
               .Where(x => x.Employment.Status == EmploymentStatus.Employed)
               .ToArray();
            (Person Resident, EducationParticipationProjection Participation)[] studentResidents = aliveResidents
               .Select(resident => (
                    Resident: resident,
                    Participation: educationParticipationIndex.FindCurrent(resident)))
               .Where(item => item.Participation is
               {
                   IsEnrolled: true
               })
               .Select(item => (
                    item.Resident,
                    Participation: item.Participation!))
               .ToArray();
            List<decimal> workforceAttendanceSamples = [];
            List<decimal> workforceProductivitySamples = [];
            List<decimal> workforceCommuteAccessibilitySamples = [];
            List<decimal> studentAttendanceSamples = [];
            List<decimal> studentCommuteAccessibilitySamples = [];

            if (includeCommuteMetrics)
            {
                foreach (Person resident in employedResidents)
                {
                    HousingStatus? residentHousingStatus = housingByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out HousingStatus resolvedHousingStatus)
                        ? resolvedHousingStatus
                        : null;
                    DistrictId? districtId = districtByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out DistrictId? resolvedDistrictId)
                        ? resolvedDistrictId
                        : null;
                    ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                        ? resolvedResidentialBuildingId
                        : null;
                    CityPopulationLivingConditionsContext districtLivingConditions =
                        districtImpactPolicy.ResolveLivingConditions(
                            districtId: districtId,
                            livingConditionsState: livingConditionsState);
                    CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                        districtId: districtId,
                        essentialsState: essentialsState);
                    CityPopulationCommuteContext commute = await commuteRoutingService.ResolveEmploymentCommuteAsync(
                        cityId: cityId.Value,
                        residentialBuildingId: residentialBuildingId,
                        resident: resident,
                        cancellationToken: cancellationToken);
                    CityPopulationParticipationProfile profile = participationPolicy.ResolveEmploymentProfile(
                        person: resident,
                        currentDate: currentDate,
                        housingStatus: residentHousingStatus,
                        livingConditions: districtLivingConditions,
                        essentials: districtEssentials,
                        commute: commute);
                    workforceAttendanceSamples.Add(profile.AttendanceIndex);
                    workforceProductivitySamples.Add(profile.ProductivityIndex);
                    workforceCommuteAccessibilitySamples.Add(commute.AccessibilityIndex);
                }

                foreach ((Person resident, EducationParticipationProjection participation) in studentResidents)
                {
                    HousingStatus? residentHousingStatus = housingByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out HousingStatus resolvedHousingStatus)
                        ? resolvedHousingStatus
                        : null;
                    DistrictId? districtId = districtByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out DistrictId? resolvedDistrictId)
                        ? resolvedDistrictId
                        : null;
                    ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                        ? resolvedResidentialBuildingId
                        : null;
                    CityPopulationLivingConditionsContext districtLivingConditions =
                        districtImpactPolicy.ResolveLivingConditions(
                            districtId: districtId,
                            livingConditionsState: livingConditionsState);
                    CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                        districtId: districtId,
                        essentialsState: essentialsState);
                    CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                        cityId: cityId.Value,
                        residentialBuildingId: residentialBuildingId,
                        destinationAnchorId: participation.InstitutionAnchorId is { } institutionAnchorId
                            ? CityAnchorId.From(institutionAnchorId)
                            : null,
                        cancellationToken: cancellationToken);
                    studentAttendanceSamples.Add(
                        participationPolicy.ResolveLearningAttendanceIndex(
                            person: resident,
                            currentDate: currentDate,
                            housingStatus: residentHousingStatus,
                            livingConditions: districtLivingConditions,
                            essentials: districtEssentials,
                            commute: commute));
                    studentCommuteAccessibilitySamples.Add(commute.AccessibilityIndex);
                }
            }

            decimal? workforceAttendanceIndex = AverageMetric(workforceAttendanceSamples);
            decimal? workforceProductivityIndex = AverageMetric(workforceProductivitySamples);
            decimal? workforceCommuteAccessibilityIndex = AverageMetric(workforceCommuteAccessibilitySamples);
            decimal? studentAttendanceIndex = AverageMetric(studentAttendanceSamples);
            decimal? studentCommuteAccessibilityIndex = AverageMetric(studentCommuteAccessibilitySamples);

            return new CityPopulationSummarySnapshotValues(
                CurrentDate: currentDate,
                UpdatedAtUtc: updatedAtUtc,
                HouseholdCount: householdPlacements.Count,
                HousedHouseholdCount: householdPlacements.Count(x => x.HousingStatus == HousingStatus.Housed),
                HomelessHouseholdCount: householdPlacements.Count(x => x.HousingStatus == HousingStatus.Homeless),
                ResidentCount: aliveResidents.Length,
                DeceasedCount: persons.Count - aliveResidents.Length,
                HousedResidentCount: aliveResidents.Count(x =>
                    housingByHouseholdId.TryGetValue(
                        key: x.HouseholdId,
                        value: out HousingStatus status) &&
                    status == HousingStatus.Housed),
                HomelessResidentCount: aliveResidents.Count(x =>
                    housingByHouseholdId.TryGetValue(
                        key: x.HouseholdId,
                        value: out HousingStatus status) &&
                    status == HousingStatus.Homeless),
                ChildCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Child),
                YouthCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Youth),
                AdultCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Adult),
                SeniorCount: aliveResidents.Count(x => x.GetAgeGroup(currentDate) == AgeGroup.Senior),
                EmployedCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Employed),
                StudentCount: studentResidents.Length,
                UnemployedCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Unemployed),
                RetiredCount: aliveResidents.Count(x => x.Employment.Status == EmploymentStatus.Retired),
                AverageHealth: aliveResidents.Select(x => (decimal?)x.Health.Value)
                   .Average(),
                AverageHappiness: aliveResidents.Select(x => (decimal?)x.Happiness.Value)
                   .Average(),
                AverageEnergy: aliveResidents.Select(x => (decimal?)x.Energy.Value)
                   .Average(),
                AverageStress: aliveResidents.Select(x => (decimal?)x.Stress.Value)
                   .Average(),
                AverageSocialNeed: aliveResidents.Select(x => (decimal?)x.SocialNeed.Value)
                   .Average(),
                ActiveIllnessCount: healthcarePressureProfile.ActiveIllnessCount,
                SevereIllnessCount: healthcarePressureProfile.SevereIllnessCount,
                MedicalLoadIndex: healthcarePressureProfile.MedicalLoadIndex,
                TriagePressureIndex: healthcarePressureProfile.TriagePressureIndex,
                RecoverySupportIndex: healthcarePressureProfile.RecoverySupportIndex,
                WorkforceCommuteAccessibilityIndex: workforceCommuteAccessibilityIndex,
                WorkforceAttendanceIndex: workforceAttendanceIndex,
                WorkforceProductivityIndex: workforceProductivityIndex,
                StudentCommuteAccessibilityIndex: studentCommuteAccessibilityIndex,
                StudentAttendanceIndex: studentAttendanceIndex);
        }

        private Task UpsertSummaryProjectionAsync(
            CityId cityId,
            CityPopulationSummarySnapshotValues snapshot,
            CancellationToken cancellationToken)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      INSERT INTO "CityPopulationSummaryProjections"
                      ("CityId", "ActiveIllnessCount", "AdultCount", "AverageEnergy", "AverageHappiness", "AverageHealth", "AverageSocialNeed", "AverageStress", "ChildCount", "CurrentDate", "DeceasedCount", "EmployedCount", "HomelessHouseholdCount", "HomelessResidentCount", "HousedHouseholdCount", "HousedResidentCount", "HouseholdCount", "MedicalLoadIndex", "RecoverySupportIndex", "ResidentCount", "RetiredCount", "SeniorCount", "SevereIllnessCount", "StudentCount", "StudentAttendanceIndex", "StudentCommuteAccessibilityIndex", "TriagePressureIndex", "UnemployedCount", "UpdatedAtUtc", "WorkforceAttendanceIndex", "WorkforceCommuteAccessibilityIndex", "WorkforceProductivityIndex", "YouthCount")
                      VALUES
                      ({cityId.Value}, {snapshot.ActiveIllnessCount}, {snapshot.AdultCount}, {snapshot.AverageEnergy}, {snapshot.AverageHappiness}, {snapshot.AverageHealth}, {snapshot.AverageSocialNeed}, {snapshot.AverageStress}, {snapshot.ChildCount}, {snapshot.CurrentDate}, {snapshot.DeceasedCount}, {snapshot.EmployedCount}, {snapshot.HomelessHouseholdCount}, {snapshot.HomelessResidentCount}, {snapshot.HousedHouseholdCount}, {snapshot.HousedResidentCount}, {snapshot.HouseholdCount}, {snapshot.MedicalLoadIndex}, {snapshot.RecoverySupportIndex}, {snapshot.ResidentCount}, {snapshot.RetiredCount}, {snapshot.SeniorCount}, {snapshot.SevereIllnessCount}, {snapshot.StudentCount}, {snapshot.StudentAttendanceIndex}, {snapshot.StudentCommuteAccessibilityIndex}, {snapshot.TriagePressureIndex}, {snapshot.UnemployedCount}, {snapshot.UpdatedAtUtc}, {snapshot.WorkforceAttendanceIndex}, {snapshot.WorkforceCommuteAccessibilityIndex}, {snapshot.WorkforceProductivityIndex}, {snapshot.YouthCount})
                      ON CONFLICT ("CityId") DO UPDATE SET
                          "ActiveIllnessCount" = EXCLUDED."ActiveIllnessCount",
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
                          "MedicalLoadIndex" = EXCLUDED."MedicalLoadIndex",
                          "RecoverySupportIndex" = EXCLUDED."RecoverySupportIndex",
                          "ResidentCount" = EXCLUDED."ResidentCount",
                          "RetiredCount" = EXCLUDED."RetiredCount",
                          "SeniorCount" = EXCLUDED."SeniorCount",
                          "SevereIllnessCount" = EXCLUDED."SevereIllnessCount",
                          "StudentCount" = EXCLUDED."StudentCount",
                          "StudentAttendanceIndex" = EXCLUDED."StudentAttendanceIndex",
                          "StudentCommuteAccessibilityIndex" = EXCLUDED."StudentCommuteAccessibilityIndex",
                          "TriagePressureIndex" = EXCLUDED."TriagePressureIndex",
                          "UnemployedCount" = EXCLUDED."UnemployedCount",
                          "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc",
                          "WorkforceAttendanceIndex" = EXCLUDED."WorkforceAttendanceIndex",
                          "WorkforceCommuteAccessibilityIndex" = EXCLUDED."WorkforceCommuteAccessibilityIndex",
                          "WorkforceProductivityIndex" = EXCLUDED."WorkforceProductivityIndex",
                          "YouthCount" = EXCLUDED."YouthCount";
                      """,
                cancellationToken: cancellationToken);
        }

        private Task UpsertDailySnapshotAsync(
            CityId cityId,
            CityPopulationSummarySnapshotValues snapshot,
            CancellationToken cancellationToken)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      INSERT INTO "CityPopulationDailySummarySnapshots"
                      ("CityId", "SnapshotDate", "ActiveIllnessCount", "AdultCount", "AverageEnergy", "AverageHappiness", "AverageHealth", "AverageSocialNeed", "AverageStress", "ChildCount", "DeceasedCount", "EmployedCount", "HomelessHouseholdCount", "HomelessResidentCount", "HousedHouseholdCount", "HousedResidentCount", "HouseholdCount", "MedicalLoadIndex", "RecoverySupportIndex", "ResidentCount", "RetiredCount", "SeniorCount", "SevereIllnessCount", "StudentCount", "StudentAttendanceIndex", "StudentCommuteAccessibilityIndex", "TriagePressureIndex", "UnemployedCount", "UpdatedAtUtc", "WorkforceAttendanceIndex", "WorkforceCommuteAccessibilityIndex", "WorkforceProductivityIndex", "YouthCount")
                      VALUES
                      ({cityId.Value}, {snapshot.CurrentDate}, {snapshot.ActiveIllnessCount}, {snapshot.AdultCount}, {snapshot.AverageEnergy}, {snapshot.AverageHappiness}, {snapshot.AverageHealth}, {snapshot.AverageSocialNeed}, {snapshot.AverageStress}, {snapshot.ChildCount}, {snapshot.DeceasedCount}, {snapshot.EmployedCount}, {snapshot.HomelessHouseholdCount}, {snapshot.HomelessResidentCount}, {snapshot.HousedHouseholdCount}, {snapshot.HousedResidentCount}, {snapshot.HouseholdCount}, {snapshot.MedicalLoadIndex}, {snapshot.RecoverySupportIndex}, {snapshot.ResidentCount}, {snapshot.RetiredCount}, {snapshot.SeniorCount}, {snapshot.SevereIllnessCount}, {snapshot.StudentCount}, {snapshot.StudentAttendanceIndex}, {snapshot.StudentCommuteAccessibilityIndex}, {snapshot.TriagePressureIndex}, {snapshot.UnemployedCount}, {snapshot.UpdatedAtUtc}, {snapshot.WorkforceAttendanceIndex}, {snapshot.WorkforceCommuteAccessibilityIndex}, {snapshot.WorkforceProductivityIndex}, {snapshot.YouthCount})
                      ON CONFLICT ("CityId", "SnapshotDate") DO UPDATE SET
                          "ActiveIllnessCount" = EXCLUDED."ActiveIllnessCount",
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
                          "MedicalLoadIndex" = EXCLUDED."MedicalLoadIndex",
                          "RecoverySupportIndex" = EXCLUDED."RecoverySupportIndex",
                          "ResidentCount" = EXCLUDED."ResidentCount",
                          "RetiredCount" = EXCLUDED."RetiredCount",
                          "SeniorCount" = EXCLUDED."SeniorCount",
                          "SevereIllnessCount" = EXCLUDED."SevereIllnessCount",
                          "StudentCount" = EXCLUDED."StudentCount",
                          "StudentAttendanceIndex" = EXCLUDED."StudentAttendanceIndex",
                          "StudentCommuteAccessibilityIndex" = EXCLUDED."StudentCommuteAccessibilityIndex",
                          "TriagePressureIndex" = EXCLUDED."TriagePressureIndex",
                          "UnemployedCount" = EXCLUDED."UnemployedCount",
                          "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc",
                          "WorkforceAttendanceIndex" = EXCLUDED."WorkforceAttendanceIndex",
                          "WorkforceCommuteAccessibilityIndex" = EXCLUDED."WorkforceCommuteAccessibilityIndex",
                          "WorkforceProductivityIndex" = EXCLUDED."WorkforceProductivityIndex",
                          "YouthCount" = EXCLUDED."YouthCount";
                      """,
                cancellationToken: cancellationToken);
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

            return await _dbContext.CityPopulationEnvironments.AsNoTracking()
                      .AnyAsync(
                           predicate: x => x.CityId == cityId,
                           cancellationToken: cancellationToken) ||
                   await _dbContext.CityPopulationProgressionStates.AsNoTracking()
                      .AnyAsync(
                           predicate: x => x.CityId == cityId,
                           cancellationToken: cancellationToken) ||
                   await _dbContext.CityPopulationWeatherExposureStates.AsNoTracking()
                      .AnyAsync(
                           predicate: x => x.CityId == cityId,
                           cancellationToken: cancellationToken) ||
                   await _dbContext.CityPopulationWeatherImpactStates.AsNoTracking()
                      .AnyAsync(
                           predicate: x => x.CityId == cityId,
                           cancellationToken: cancellationToken) ||
                   await _dbContext.CityPopulationArchiveStates.AsNoTracking()
                      .AnyAsync(
                           predicate: x => x.CityId == cityId,
                           cancellationToken: cancellationToken) ||
                   await _dbContext.CityPopulationDeletionStates.AsNoTracking()
                      .AnyAsync(
                           predicate: x => x.CityId == cityId,
                           cancellationToken: cancellationToken);
        }

        private async Task<(int ResidentCount, int HouseholdCount)> GetPersistedPopulationCountsAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            int householdCount = await _dbContext.ClassicCityHouseholdPlacements
               .AsNoTracking()
               .CountAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);

            if (householdCount == 0)
                return (0, 0);

            int residentCount = await _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements
                       .AsNoTracking()
                       .Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .CountAsync(cancellationToken);

            return (residentCount, householdCount);
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
                : DateOnly.FromDateTime(
                    _timeProvider.GetUtcNow()
                       .UtcDateTime);
        }

        private static decimal? AverageMetric(IReadOnlyCollection<decimal> values)
        {
            return values.Count == 0
                ? null
                : decimal.Round(
                    d: values.Average(),
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero);
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
            decimal? AverageSocialNeed,
            int ActiveIllnessCount,
            int SevereIllnessCount,
            decimal? MedicalLoadIndex,
            decimal? TriagePressureIndex,
            decimal? RecoverySupportIndex,
            decimal? WorkforceCommuteAccessibilityIndex,
            decimal? WorkforceAttendanceIndex,
            decimal? WorkforceProductivityIndex,
            decimal? StudentCommuteAccessibilityIndex,
            decimal? StudentAttendanceIndex);
    }
}

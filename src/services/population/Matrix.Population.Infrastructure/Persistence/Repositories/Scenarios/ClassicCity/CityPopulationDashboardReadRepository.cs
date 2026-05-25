using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationDashboardReadRepository(
        PopulationDbContext dbContext,
        CityHouseholdEconomyPolicy householdEconomyPolicy,
        CityHouseholdCashflowPolicy householdCashflowPolicy,
        CityPopulationDistrictImpactPolicy districtImpactPolicy,
        CityPopulationParticipationPolicy participationPolicy)
        : ICityPopulationDashboardReadRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;
        private readonly CityPopulationDistrictImpactPolicy _districtImpactPolicy = districtImpactPolicy;
        private readonly CityHouseholdCashflowPolicy _householdCashflowPolicy = householdCashflowPolicy;
        private readonly CityHouseholdEconomyPolicy _householdEconomyPolicy = householdEconomyPolicy;
        private readonly CityPopulationParticipationPolicy _participationPolicy = participationPolicy;

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
                    averageSocialNeed: projection.AverageSocialNeed,
                    activeIllnessCount: projection.ActiveIllnessCount,
                    severeIllnessCount: projection.SevereIllnessCount,
                    medicalLoadIndex: projection.MedicalLoadIndex,
                    triagePressureIndex: projection.TriagePressureIndex,
                    recoverySupportIndex: projection.RecoverySupportIndex,
                    workforceCommuteAccessibilityIndex: projection.WorkforceCommuteAccessibilityIndex,
                    workforceAttendanceIndex: projection.WorkforceAttendanceIndex,
                    workforceProductivityIndex: projection.WorkforceProductivityIndex,
                    studentCommuteAccessibilityIndex: projection.StudentCommuteAccessibilityIndex,
                    studentAttendanceIndex: projection.StudentAttendanceIndex);
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
                    averageSocialNeed: snapshot.AverageSocialNeed,
                    activeIllnessCount: snapshot.ActiveIllnessCount,
                    severeIllnessCount: snapshot.SevereIllnessCount,
                    medicalLoadIndex: snapshot.MedicalLoadIndex,
                    triagePressureIndex: snapshot.TriagePressureIndex,
                    recoverySupportIndex: snapshot.RecoverySupportIndex,
                    workforceCommuteAccessibilityIndex: snapshot.WorkforceCommuteAccessibilityIndex,
                    workforceAttendanceIndex: snapshot.WorkforceAttendanceIndex,
                    workforceProductivityIndex: snapshot.WorkforceProductivityIndex,
                    studentCommuteAccessibilityIndex: snapshot.StudentCommuteAccessibilityIndex,
                    studentAttendanceIndex: snapshot.StudentAttendanceIndex);
        }

        public async Task<CityPopulationDashboardEconomyReadModel> GetCurrentEconomySnapshotAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default)
        {
            ClassicCityHouseholdPlacement[] placements = await _dbContext.ClassicCityHouseholdPlacements
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .ToArrayAsync(cancellationToken);

            if (placements.Length == 0)
                return new CityPopulationDashboardEconomyReadModel(
                    StableHouseholdCount: 0,
                    StrainedHouseholdCount: 0,
                    DeficitHouseholdCount: 0,
                    AverageCashReserveAmount: null,
                    AverageDailyNetAmount: null);

            HouseholdId[] householdIds = placements
               .Select(x => x.HouseholdId)
               .Distinct()
               .ToArray();

            Household[] households = await _dbContext.Households
               .AsNoTracking()
               .Where(x => householdIds.Contains(x.Id))
               .ToArrayAsync(cancellationToken);
            var householdsById = households.ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);

            Person[] persons = await _dbContext.Persons
               .AsNoTracking()
               .Where(x => x.Life.Status == LifeStatus.Alive && householdIds.Contains(x.HouseholdId))
               .ToArrayAsync(cancellationToken);

            var residentsByHousehold = persons
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.ToArray());
            CityPopulationCostOfLivingState? costOfLivingState = await _dbContext.CityPopulationCostOfLivingStates
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.CityId == cityId,
                    cancellationToken: cancellationToken);
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

            int stableHouseholdCount = 0;
            int strainedHouseholdCount = 0;
            int deficitHouseholdCount = 0;
            decimal cashReserveTotal = 0m;
            decimal dailyNetTotal = 0m;
            int measuredHouseholdCount = 0;

            foreach (ClassicCityHouseholdPlacement placement in placements)
            {
                if (!householdsById.TryGetValue(
                        key: placement.HouseholdId,
                        value: out Household? household))
                    continue;

                if (!residentsByHousehold.TryGetValue(
                        key: placement.HouseholdId,
                        value: out Person[]? householdResidents) ||
                    householdResidents is null ||
                    householdResidents.Length == 0)
                    continue;

                CityHouseholdEconomyProfile economyProfile = _householdEconomyPolicy.Build(
                    household: household,
                    householdResidents: householdResidents,
                    housingStatus: placement.HousingStatus,
                    currentDate: currentDate,
                    costOfLivingState: costOfLivingState);
                decimal adjustedNetDailyIncomeAmount = 0m;
                foreach (Person resident in householdResidents)
                {
                    DistrictId? districtId = placement.DistrictId;
                    CityPopulationLivingConditionsContext districtLivingConditions =
                        _districtImpactPolicy.ResolveLivingConditions(
                            districtId: districtId,
                            livingConditionsState: livingConditionsState);
                    CityPopulationEssentialsContext districtEssentials = _districtImpactPolicy.ResolveEssentials(
                        districtId: districtId,
                        essentialsState: essentialsState);
                    // The dashboard economy snapshot is a lightweight read model. Keep it
                    // deterministic and cheap by avoiding live route resolution per resident.
                    decimal incomeMultiplier = resident.Employment.Status == EmploymentStatus.Employed
                        ? _participationPolicy.ResolveEmploymentProfile(
                                person: resident,
                                currentDate: currentDate,
                                housingStatus: placement.HousingStatus,
                                livingConditions: districtLivingConditions,
                                essentials: districtEssentials,
                                commute: CityPopulationCommuteContext.Neutral)
                           .PayrollMultiplier
                        : 1m;

                    adjustedNetDailyIncomeAmount += _householdCashflowPolicy.BuildResidentIncome(
                            resident: resident,
                            currentDate: currentDate,
                            costOfLivingState: costOfLivingState,
                            incomeMultiplier: incomeMultiplier)
                       .NetIncome.Amount;
                }

                decimal adjustedDailyNetAmount = decimal.Round(
                    d: adjustedNetDailyIncomeAmount - economyProfile.DailyExpenseAmount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero);

                if (economyProfile.StrainScore >= 0.55d)
                    strainedHouseholdCount++;
                else
                    if (economyProfile.GrowthReadinessScore >= 0.60d && economyProfile.EconomicBalance >= 0d)
                        stableHouseholdCount++;

                if (economyProfile.HasCashDeficit || adjustedDailyNetAmount < 0m)
                    deficitHouseholdCount++;

                cashReserveTotal += economyProfile.CashReserveAmount;
                dailyNetTotal += adjustedDailyNetAmount;
                measuredHouseholdCount++;
            }

            return new CityPopulationDashboardEconomyReadModel(
                StableHouseholdCount: stableHouseholdCount,
                StrainedHouseholdCount: strainedHouseholdCount,
                DeficitHouseholdCount: deficitHouseholdCount,
                AverageCashReserveAmount: measuredHouseholdCount > 0
                    ? decimal.Round(
                        d: cashReserveTotal / measuredHouseholdCount,
                        decimals: 2,
                        mode: MidpointRounding.AwayFromZero)
                    : null,
                AverageDailyNetAmount: measuredHouseholdCount > 0
                    ? decimal.Round(
                        d: dailyNetTotal / measuredHouseholdCount,
                        decimals: 2,
                        mode: MidpointRounding.AwayFromZero)
                    : null);
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
                    x.PrimaryResidentId == null
                        ? null
                        : x.PrimaryResidentId.Value.Value,
                    x.SecondaryResidentId == null
                        ? null
                        : x.SecondaryResidentId.Value.Value))
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
            decimal? averageSocialNeed,
            int activeIllnessCount,
            int severeIllnessCount,
            decimal? medicalLoadIndex,
            decimal? triagePressureIndex,
            decimal? recoverySupportIndex,
            decimal? workforceCommuteAccessibilityIndex,
            decimal? workforceAttendanceIndex,
            decimal? workforceProductivityIndex,
            decimal? studentCommuteAccessibilityIndex,
            decimal? studentAttendanceIndex)
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
                AverageSocialNeed: averageSocialNeed,
                ActiveIllnessCount: activeIllnessCount,
                SevereIllnessCount: severeIllnessCount,
                MedicalLoadIndex: medicalLoadIndex,
                TriagePressureIndex: triagePressureIndex,
                RecoverySupportIndex: recoverySupportIndex,
                WorkforceCommuteAccessibilityIndex: workforceCommuteAccessibilityIndex,
                WorkforceAttendanceIndex: workforceAttendanceIndex,
                WorkforceProductivityIndex: workforceProductivityIndex,
                StudentCommuteAccessibilityIndex: studentCommuteAccessibilityIndex,
                StudentAttendanceIndex: studentAttendanceIndex);
        }
    }
}

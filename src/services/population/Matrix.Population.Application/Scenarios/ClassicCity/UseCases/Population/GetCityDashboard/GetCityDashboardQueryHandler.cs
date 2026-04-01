using System.Globalization;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed class GetCityDashboardQueryHandler(
        ICityPopulationSummaryProjectionService summaryProjectionService,
        ICityPopulationDashboardReadRepository dashboardReadRepository)
        : IRequestHandler<GetCityDashboardQuery, CityPopulationDashboardDto?>
    {
        public async Task<CityPopulationDashboardDto?> Handle(
            GetCityDashboardQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = CityId.From(request.CityId);

            await summaryProjectionService.EnsureExistsAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            CityPopulationDashboardSnapshotReadModel? currentSnapshot =
                await dashboardReadRepository.GetCurrentSnapshotAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (currentSnapshot is null)
                return null;

            DateOnly yesterdayAnchor = currentSnapshot.SnapshotDate.AddDays(-1);
            DateOnly monthAnchor = currentSnapshot.SnapshotDate.AddMonths(-1);
            DateOnly yearAnchor = currentSnapshot.SnapshotDate.AddYears(-1);

            CityPopulationDashboardSnapshotReadModel? yesterdaySnapshot =
                await dashboardReadRepository.GetSnapshotOnOrBeforeAsync(
                    cityId: cityId,
                    snapshotDate: yesterdayAnchor,
                    cancellationToken: cancellationToken);
            CityPopulationDashboardSnapshotReadModel? previousMonthSnapshot =
                await dashboardReadRepository.GetSnapshotOnOrBeforeAsync(
                    cityId: cityId,
                    snapshotDate: monthAnchor,
                    cancellationToken: cancellationToken);
            CityPopulationDashboardSnapshotReadModel? previousYearSnapshot =
                await dashboardReadRepository.GetSnapshotOnOrBeforeAsync(
                    cityId: cityId,
                    snapshotDate: yearAnchor,
                    cancellationToken: cancellationToken);
            CityPopulationDashboardEconomyReadModel economySnapshot =
                await dashboardReadRepository.GetCurrentEconomySnapshotAsync(
                    cityId: cityId,
                    currentDate: currentSnapshot.SnapshotDate,
                    cancellationToken: cancellationToken);

            IReadOnlyList<CityPopulationActivityEventReadModel> events =
                await dashboardReadRepository.ListRecentActivityAsync(
                    cityId: cityId,
                    take: 12,
                    cancellationToken: cancellationToken);

            return new CityPopulationDashboardDto(
                CityId: currentSnapshot.CityId,
                CurrentDate: FormatDate(currentSnapshot.SnapshotDate),
                GeneratedAtUtc: FormatTimestamp(DateTimeOffset.UtcNow)!,
                Metrics:
                [
                    CreateCountMetric(
                        key: "residents",
                        label: "Residents",
                        description: "Alive residents currently tracked inside this city simulation.",
                        currentValue: currentSnapshot.ResidentCount,
                        yesterdayValue: yesterdaySnapshot?.ResidentCount,
                        monthValue: previousMonthSnapshot?.ResidentCount,
                        yearValue: previousYearSnapshot?.ResidentCount),
                    CreateCountMetric(
                        key: "households",
                        label: "Households",
                        description: "Resident households currently placed inside the classic city host.",
                        currentValue: currentSnapshot.HouseholdCount,
                        yesterdayValue: yesterdaySnapshot?.HouseholdCount,
                        monthValue: previousMonthSnapshot?.HouseholdCount,
                        yearValue: previousYearSnapshot?.HouseholdCount),
                    CreateCountMetric(
                        key: "homelessResidents",
                        label: "Homeless residents",
                        description: "Residents without housed household placement at the current simulation date.",
                        currentValue: currentSnapshot.HomelessResidentCount,
                        yesterdayValue: yesterdaySnapshot?.HomelessResidentCount,
                        monthValue: previousMonthSnapshot?.HomelessResidentCount,
                        yearValue: previousYearSnapshot?.HomelessResidentCount),
                    CreateCountMetric(
                        key: "employed",
                        label: "Employed",
                        description: "Residents with an active workplace assignment right now.",
                        currentValue: currentSnapshot.EmployedCount,
                        yesterdayValue: yesterdaySnapshot?.EmployedCount,
                        monthValue: previousMonthSnapshot?.EmployedCount,
                        yearValue: previousYearSnapshot?.EmployedCount),
                    CreateCountMetric(
                        key: "students",
                        label: "Students",
                        description: "Residents currently assigned to study instead of regular employment.",
                        currentValue: currentSnapshot.StudentCount,
                        yesterdayValue: yesterdaySnapshot?.StudentCount,
                        monthValue: previousMonthSnapshot?.StudentCount,
                        yearValue: previousYearSnapshot?.StudentCount),
                    CreateAverageMetric(
                        key: "averageHealth",
                        label: "Average health",
                        description: "Mean health level across the alive resident population.",
                        currentValue: currentSnapshot.AverageHealth,
                        yesterdayValue: yesterdaySnapshot?.AverageHealth,
                        monthValue: previousMonthSnapshot?.AverageHealth,
                        yearValue: previousYearSnapshot?.AverageHealth),
                    CreateAverageMetric(
                        key: "averageHappiness",
                        label: "Average happiness",
                        description: "Mean happiness level across the alive resident population.",
                        currentValue: currentSnapshot.AverageHappiness,
                        yesterdayValue: yesterdaySnapshot?.AverageHappiness,
                        monthValue: previousMonthSnapshot?.AverageHappiness,
                        yearValue: previousYearSnapshot?.AverageHappiness),
                    CreateAverageMetric(
                        key: "averageStress",
                        label: "Average stress",
                        description: "Mean stress load currently carried by alive residents.",
                        currentValue: currentSnapshot.AverageStress,
                        yesterdayValue: yesterdaySnapshot?.AverageStress,
                        monthValue: previousMonthSnapshot?.AverageStress,
                        yearValue: previousYearSnapshot?.AverageStress),
                    CreateCountMetric(
                        key: "activeIllnesses",
                        label: "Active illnesses",
                        description: "Residents currently carrying any active illness burden inside the city population.",
                        currentValue: currentSnapshot.ActiveIllnessCount,
                        yesterdayValue: yesterdaySnapshot?.ActiveIllnessCount,
                        monthValue: previousMonthSnapshot?.ActiveIllnessCount,
                        yearValue: previousYearSnapshot?.ActiveIllnessCount),
                    CreateCountMetric(
                        key: "severeIllnesses",
                        label: "Severe illnesses",
                        description: "Residents currently in severe illness state and most likely to compete for urgent care.",
                        currentValue: currentSnapshot.SevereIllnessCount,
                        yesterdayValue: yesterdaySnapshot?.SevereIllnessCount,
                        monthValue: previousMonthSnapshot?.SevereIllnessCount,
                        yearValue: previousYearSnapshot?.SevereIllnessCount),
                    CreateAverageMetric(
                        key: "medicalLoad",
                        label: "Medical load",
                        description: "Weighted clinical pressure on the city's care system after illness mix, shortages, and access disruption.",
                        currentValue: currentSnapshot.MedicalLoadIndex,
                        yesterdayValue: yesterdaySnapshot?.MedicalLoadIndex,
                        monthValue: previousMonthSnapshot?.MedicalLoadIndex,
                        yearValue: previousYearSnapshot?.MedicalLoadIndex),
                    CreateAverageMetric(
                        key: "triagePressure",
                        label: "Triage pressure",
                        description: "Severity-driven pressure that forces the city to prioritize the sickest residents over routine recovery.",
                        currentValue: currentSnapshot.TriagePressureIndex,
                        yesterdayValue: yesterdaySnapshot?.TriagePressureIndex,
                        monthValue: previousMonthSnapshot?.TriagePressureIndex,
                        yearValue: previousYearSnapshot?.TriagePressureIndex),
                    CreateAverageMetric(
                        key: "recoverySupport",
                        label: "Recovery support",
                        description: "Current practical recovery support after healthcare quality, access, medicines, and overload are combined.",
                        currentValue: currentSnapshot.RecoverySupportIndex,
                        yesterdayValue: yesterdaySnapshot?.RecoverySupportIndex,
                        monthValue: previousMonthSnapshot?.RecoverySupportIndex,
                        yearValue: previousYearSnapshot?.RecoverySupportIndex),
                    CreateAverageMetric(
                        key: "workforceAttendance",
                        label: "Workforce attendance",
                        description: "Average attendance readiness of employed residents under current city conditions.",
                        currentValue: currentSnapshot.WorkforceAttendanceIndex,
                        yesterdayValue: yesterdaySnapshot?.WorkforceAttendanceIndex,
                        monthValue: previousMonthSnapshot?.WorkforceAttendanceIndex,
                        yearValue: previousYearSnapshot?.WorkforceAttendanceIndex),
                    CreateAverageMetric(
                        key: "workforceProductivity",
                        label: "Workforce productivity",
                        description: "Average effective productivity of employed residents after utility and shortage pressure.",
                        currentValue: currentSnapshot.WorkforceProductivityIndex,
                        yesterdayValue: yesterdaySnapshot?.WorkforceProductivityIndex,
                        monthValue: previousMonthSnapshot?.WorkforceProductivityIndex,
                        yearValue: previousYearSnapshot?.WorkforceProductivityIndex),
                    CreateAverageMetric(
                        key: "studentAttendance",
                        label: "Student attendance",
                        description: "Average study attendance readiness under current infrastructure and essentials conditions.",
                        currentValue: currentSnapshot.StudentAttendanceIndex,
                        yesterdayValue: yesterdaySnapshot?.StudentAttendanceIndex,
                        monthValue: previousMonthSnapshot?.StudentAttendanceIndex,
                        yearValue: previousYearSnapshot?.StudentAttendanceIndex),
                    new CityPopulationDashboardMetricDto(
                        Key: "stableHouseholds",
                        Label: "Stable households",
                        Description:
                        "Households whose current employment and dependency mix can comfortably support daily city living.",
                        ValueKind: "count",
                        CurrentValue: economySnapshot.StableHouseholdCount,
                        DeltaYesterday: null,
                        DeltaMonth: null,
                        DeltaYear: null),
                    new CityPopulationDashboardMetricDto(
                        Key: "strainedHouseholds",
                        Label: "Strained households",
                        Description:
                        "Households currently under economic pressure from low support capacity, dependents, illness, or housing load.",
                        ValueKind: "count",
                        CurrentValue: economySnapshot.StrainedHouseholdCount,
                        DeltaYesterday: null,
                        DeltaMonth: null,
                        DeltaYear: null),
                    CreateCountMetric(
                        key: "deficitHouseholds",
                        label: "Households in deficit",
                        description:
                        "Households whose current cash reserve or daily household net is already below zero.",
                        currentValue: economySnapshot.DeficitHouseholdCount,
                        yesterdayValue: null,
                        monthValue: null,
                        yearValue: null),
                    CreateAverageMetric(
                        key: "averageCashReserve",
                        label: "Average cash reserve",
                        description: "Mean money reserve currently held by resident households after daily settlement.",
                        currentValue: economySnapshot.AverageCashReserveAmount,
                        yesterdayValue: null,
                        monthValue: null,
                        yearValue: null),
                    CreateAverageMetric(
                        key: "averageDailyNet",
                        label: "Average daily net",
                        description: "Mean daily household net after take-home income, taxes, and living expenses.",
                        currentValue: economySnapshot.AverageDailyNetAmount,
                        yesterdayValue: null,
                        monthValue: null,
                        yearValue: null)
                ],
                RecentEvents: events
                   .Select(x => new CityPopulationActivityEventDto(
                        ActivityEventId: x.ActivityEventId,
                        CurrentDate: FormatDate(x.CurrentDate),
                        OccurredAtUtc: FormatTimestamp(x.OccurredAtUtc)!,
                        EventType: x.EventType,
                        Source: x.Source,
                        Severity: x.Severity,
                        Title: x.Title,
                        Summary: x.Summary,
                        PrimaryResidentId: x.PrimaryResidentId,
                        SecondaryResidentId: x.SecondaryResidentId))
                   .ToArray());
        }

        private static CityPopulationDashboardMetricDto CreateCountMetric(
            string key,
            string label,
            string description,
            int currentValue,
            int? yesterdayValue,
            int? monthValue,
            int? yearValue)
        {
            return new CityPopulationDashboardMetricDto(
                Key: key,
                Label: label,
                Description: description,
                ValueKind: "count",
                CurrentValue: currentValue,
                DeltaYesterday: CreateDelta(
                    currentValue: currentValue,
                    previousValue: yesterdayValue),
                DeltaMonth: CreateDelta(
                    currentValue: currentValue,
                    previousValue: monthValue),
                DeltaYear: CreateDelta(
                    currentValue: currentValue,
                    previousValue: yearValue));
        }

        private static CityPopulationDashboardMetricDto CreateAverageMetric(
            string key,
            string label,
            string description,
            decimal? currentValue,
            decimal? yesterdayValue,
            decimal? monthValue,
            decimal? yearValue)
        {
            decimal roundedCurrent = RoundMetric(currentValue) ?? 0m;

            return new CityPopulationDashboardMetricDto(
                Key: key,
                Label: label,
                Description: description,
                ValueKind: "average",
                CurrentValue: roundedCurrent,
                DeltaYesterday: CreateDelta(
                    currentValue: roundedCurrent,
                    previousValue: RoundMetric(yesterdayValue)),
                DeltaMonth: CreateDelta(
                    currentValue: roundedCurrent,
                    previousValue: RoundMetric(monthValue)),
                DeltaYear: CreateDelta(
                    currentValue: roundedCurrent,
                    previousValue: RoundMetric(yearValue)));
        }

        private static decimal? CreateDelta(
            decimal currentValue,
            decimal? previousValue)
        {
            return previousValue.HasValue
                ? RoundMetric(currentValue - previousValue.Value)
                : null;
        }

        private static decimal? CreateDelta(
            int currentValue,
            int? previousValue)
        {
            return previousValue.HasValue
                ? currentValue - previousValue.Value
                : null;
        }

        private static decimal? RoundMetric(decimal? value)
        {
            return value.HasValue
                ? decimal.Round(
                    d: value.Value,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero)
                : null;
        }

        private static string FormatDate(DateOnly value)
        {
            return value.ToString(
                format: "yyyy-MM-dd",
                provider: CultureInfo.InvariantCulture);
        }

        private static string? FormatTimestamp(DateTimeOffset? value)
        {
            return value?.ToString(
                format: "O",
                formatProvider: CultureInfo.InvariantCulture);
        }
    }
}

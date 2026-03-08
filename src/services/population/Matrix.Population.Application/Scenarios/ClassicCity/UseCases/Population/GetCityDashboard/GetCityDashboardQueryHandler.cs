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
            CityId cityId = CityId.From(request.CityId);

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
                DeltaYesterday: CreateDelta(currentValue, yesterdayValue),
                DeltaMonth: CreateDelta(currentValue, monthValue),
                DeltaYear: CreateDelta(currentValue, yearValue));
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
                DeltaYesterday: CreateDelta(roundedCurrent, RoundMetric(yesterdayValue)),
                DeltaMonth: CreateDelta(roundedCurrent, RoundMetric(monthValue)),
                DeltaYear: CreateDelta(roundedCurrent, RoundMetric(yearValue)));
        }

        private static decimal? CreateDelta(decimal currentValue, decimal? previousValue)
        {
            return previousValue.HasValue
                ? RoundMetric(currentValue - previousValue.Value)
                : null;
        }

        private static decimal? CreateDelta(int currentValue, int? previousValue)
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

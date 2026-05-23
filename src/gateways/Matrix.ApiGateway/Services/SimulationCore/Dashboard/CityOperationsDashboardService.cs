using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal sealed class CityOperationsDashboardService(
        ICitiesApiClient citiesClient,
        ICityOperationsDashboardHealthProbe healthProbe,
        ICityOperationsDashboardSnapshotLoader snapshotLoader,
        ICityOperationsDashboardAlertBuilder alertBuilder,
        ICityOperationsDashboardRecentEventsBuilder recentEventsBuilder,
        TimeProvider timeProvider) : ICityOperationsDashboardService
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly ICityOperationsDashboardHealthProbe _healthProbe = healthProbe;
        private readonly ICityOperationsDashboardSnapshotLoader _snapshotLoader = snapshotLoader;
        private readonly ICityOperationsDashboardAlertBuilder _alertBuilder = alertBuilder;
        private readonly ICityOperationsDashboardRecentEventsBuilder _recentEventsBuilder = recentEventsBuilder;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken)
        {
            Task<IReadOnlyList<CityListItemView>> allCitiesTask = _citiesClient.ListCitiesAsync(
                includeArchived: true,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<CityListItemView>> provisioningTask = _citiesClient.ListProvisioningCitiesAsync(
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<DashboardServiceHealthView>> healthTask = _healthProbe.ProbeAsync(cancellationToken);

            await Task.WhenAll(
                allCitiesTask,
                provisioningTask,
                healthTask);

            IReadOnlyList<CityListItemView> allCities = await allCitiesTask;
            IReadOnlyList<CityListItemView> attentionCities = await provisioningTask;
            IReadOnlyList<DashboardServiceHealthView> services = await healthTask;
            IReadOnlyList<CityOperationalSnapshot> operationalSnapshots =
                await _snapshotLoader.LoadReadyClassicCitySnapshotsAsync(
                    allCities: allCities,
                    cancellationToken: cancellationToken);
            CityOperationsDashboardAlerts alerts = _alertBuilder.Build(operationalSnapshots);

            DashboardEnvironmentalAlertView[] environmentalAlerts = alerts.EnvironmentalAlerts;
            DashboardPopulationDistrictPressureView[] populationDistrictAlerts = alerts.PopulationDistrictAlerts;
            DashboardDistrictResponsePriorityView[] districtResponsePriorities = alerts.DistrictResponsePriorities;
            DashboardMobilityView[] mobilityAlerts = alerts.MobilityAlerts;
            DashboardBudgetPressureView[] budgetAlerts = alerts.BudgetAlerts;
            DashboardTickFreshnessView[] tickFreshnessAlerts = alerts.TickFreshnessAlerts;
            DashboardPhaseProgressView[] phaseProgressAlerts = alerts.PhaseProgressAlerts;
            DateTimeOffset generatedAtUtc = _timeProvider.GetUtcNow();
            DateTimeOffset localNow = _timeProvider.GetLocalNow();

            CityListItemView[] readyCities = allCities
               .Where(IsReady)
               .OrderByDescending(city => city.PopulationBootstrapCompletedAtUtc ?? city.CreatedAtUtc)
               .ThenBy(
                    keySelector: city => city.Name,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .Take(6)
               .ToArray();

            CityListItemView[] archivedCities = allCities
               .Where(IsArchived)
               .OrderByDescending(city => city.ArchivedAtUtc ?? DateTimeOffset.MinValue)
               .ThenBy(
                    keySelector: city => city.Name,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .Take(6)
               .ToArray();

            CityListItemView[] rankedAttentionCities = attentionCities
               .OrderBy(city => GetAttentionRank(city))
               .ThenByDescending(city => city.PopulationBootstrapFailedAtUtc ?? city.CreatedAtUtc)
               .ThenBy(
                    keySelector: city => city.Name,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();

            return new CityOperationsDashboardView(
                GeneratedAtUtc: generatedAtUtc,
                TrackedHosts: BuildSnapshotMetric(
                    label: "Tracked hosts",
                    description:
                    "Total city records still visible to operators across live, provisioning, and archived workspaces.",
                    current: allCities.Count,
                    countAtCutoff: cutoff => allCities.Count(city => city.CreatedAtUtc <= cutoff)),
                ReadyHosts: BuildSnapshotMetric(
                    label: "Ready monitoring",
                    description: "Cities that have completed bootstrap and are currently live for direct monitoring.",
                    current: allCities.Count(IsReady),
                    countAtCutoff: cutoff => allCities.Count(city => WasReadyAt(
                        city: city,
                        cutoff: cutoff))),
                ArchivedRecords: BuildSnapshotMetric(
                    label: "Archived records",
                    description: "Historical city records retained for audit, cleanup, and post-mortem review.",
                    current: allCities.Count(IsArchived),
                    countAtCutoff: cutoff => allCities.Count(city => city.ArchivedAtUtc is
                                                                         { } archivedAtUtc &&
                                                                     archivedAtUtc <= cutoff)),
                AttentionQueue: new DashboardMetricView(
                    Label: "Attention queue",
                    Current: rankedAttentionCities.Length,
                    Description: "Provisioning handoffs or failed launches that still need active operator follow-up.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                EnvironmentalAlerts: new DashboardMetricView(
                    Label: "Environmental alerts",
                    Current: environmentalAlerts.Length,
                    Description:
                    "Ready classic-city simulations currently showing flooding, snow pressure, utility disruption, power loss, degraded road access, or supply-chain stress.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                PopulationDistrictAlerts: new DashboardMetricView(
                    Label: "District population alerts",
                    Current: populationDistrictAlerts.Length,
                    Description:
                    "Ready classic-city simulations where one or more districts are showing acute population pressure from illness, housing fragility, or unstable utilities.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                DistrictResponsePriorityAlerts: new DashboardMetricView(
                    Label: "District response priorities",
                    Current: districtResponsePriorities.Length,
                    Description:
                    "District-level operator priorities where social pressure and utility instability are converging into the next best response target.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                MobilityAlerts: new DashboardMetricView(
                    Label: "Mobility alerts",
                    Current: mobilityAlerts.Length,
                    Description:
                    "Ready classic-city simulations where active commute and healthcare trips are starting to stack up under slower or more fragile movement conditions.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                OperationalBudgetAlerts: new DashboardMetricView(
                    Label: "Operational budget alerts",
                    Current: budgetAlerts.Length,
                    Description:
                    "Ready classic-city simulations where municipal operations spending or category budget caps are starting to constrain city response.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                TickFreshnessAlerts: new DashboardMetricView(
                    Label: "Tick freshness alerts",
                    Current: tickFreshnessAlerts.Length,
                    Description:
                    "Ready classic-city simulations where budget and environmental snapshots have started to drift apart by multiple world ticks.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                PhaseProgressAlerts: new DashboardMetricView(
                    Label: "Phase progress alerts",
                    Current: phaseProgressAlerts.Length,
                    Description:
                    "Ready classic-city simulations where systems, resource settlement, and budget settlement are no longer progressing through the same world tick pipeline cleanly.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                NewCities: BuildPeriodComparisonRow(
                    label: "New cities",
                    description: "Fresh hosts entering the system through the setup and provisioning pipeline.",
                    selectMoment: city => city.CreatedAtUtc,
                    now: localNow,
                    source: allCities),
                ArchivedCities: BuildPeriodComparisonRow(
                    label: "Archived cities",
                    description: "Hosts moved out of active monitoring and kept only as records.",
                    selectMoment: city => city.ArchivedAtUtc,
                    now: localNow,
                    source: allCities),
                FailedBootstraps: BuildPeriodComparisonRow(
                    label: "Failed bootstraps",
                    description: "Population bootstrap failures that interrupted a city before it became ready.",
                    selectMoment: city => city.PopulationBootstrapFailedAtUtc,
                    now: localNow,
                    source: allCities),
                ReadyHandOffs: BuildPeriodComparisonRow(
                    label: "Ready handoffs",
                    description: "Cities that completed provisioning and became available for monitoring.",
                    selectMoment: city => city.PopulationBootstrapCompletedAtUtc,
                    now: localNow,
                    source: allCities),
                Services: services,
                Events: _recentEventsBuilder.Build(allCities),
                EnvironmentalCities: environmentalAlerts.Take(8)
                   .ToArray(),
                PopulationDistrictCities: populationDistrictAlerts.Take(8)
                   .ToArray(),
                DistrictResponsePriorities: districtResponsePriorities.Take(8)
                   .ToArray(),
                MobilityCities: mobilityAlerts.Take(8)
                   .ToArray(),
                BudgetPressureCities: budgetAlerts.Take(8)
                   .ToArray(),
                TickFreshnessCities: tickFreshnessAlerts.Take(8)
                   .ToArray(),
                PhaseProgressCities: phaseProgressAlerts.Take(8)
                   .ToArray(),
                AttentionCities: rankedAttentionCities.Take(8)
                   .ToArray(),
                ReadyCities: readyCities,
                ArchivedCitiesList: archivedCities);
        }

        private DashboardMetricView BuildSnapshotMetric(
            string label,
            string description,
            int current,
            Func<DateTimeOffset, int> countAtCutoff)
        {
            DateTimeOffset now = _timeProvider.GetLocalNow();
            DateTimeOffset dayStart = new(
                year: now.Year,
                month: now.Month,
                day: now.Day,
                hour: 0,
                minute: 0,
                second: 0,
                offset: now.Offset);
            DateTimeOffset monthStart = new(
                year: now.Year,
                month: now.Month,
                day: 1,
                hour: 0,
                minute: 0,
                second: 0,
                offset: now.Offset);
            DateTimeOffset yearStart = new(
                year: now.Year,
                month: 1,
                day: 1,
                hour: 0,
                minute: 0,
                second: 0,
                offset: now.Offset);

            return new DashboardMetricView(
                Label: label,
                Current: current,
                Description: description,
                DeltaYesterday: current - countAtCutoff(dayStart),
                DeltaMonth: current - countAtCutoff(monthStart),
                DeltaYear: current - countAtCutoff(yearStart),
                DeltaMode: "net");
        }

        private static DashboardPeriodComparisonRowView BuildPeriodComparisonRow(
            string label,
            string description,
            Func<CityListItemView, DateTimeOffset?> selectMoment,
            DateTimeOffset now,
            IReadOnlyList<CityListItemView> source)
        {
            DateTimeOffset dayStart = new(
                year: now.Year,
                month: now.Month,
                day: now.Day,
                hour: 0,
                minute: 0,
                second: 0,
                offset: now.Offset);
            DateTimeOffset monthStart = new(
                year: now.Year,
                month: now.Month,
                day: 1,
                hour: 0,
                minute: 0,
                second: 0,
                offset: now.Offset);
            DateTimeOffset yearStart = new(
                year: now.Year,
                month: 1,
                day: 1,
                hour: 0,
                minute: 0,
                second: 0,
                offset: now.Offset);

            DateTimeOffset previousDayStart = dayStart.AddDays(-1);
            DateTimeOffset previousMonthStart = monthStart.AddMonths(-1);
            DateTimeOffset previousYearStart = yearStart.AddYears(-1);

            return new DashboardPeriodComparisonRowView(
                Label: label,
                Description: description,
                Yesterday: BuildWindowComparison(
                    source: source,
                    selectMoment: selectMoment,
                    currentStart: dayStart,
                    currentEnd: now,
                    previousStart: previousDayStart,
                    previousEnd: dayStart),
                Month: BuildWindowComparison(
                    source: source,
                    selectMoment: selectMoment,
                    currentStart: monthStart,
                    currentEnd: now,
                    previousStart: previousMonthStart,
                    previousEnd: monthStart),
                Year: BuildWindowComparison(
                    source: source,
                    selectMoment: selectMoment,
                    currentStart: yearStart,
                    currentEnd: now,
                    previousStart: previousYearStart,
                    previousEnd: yearStart));
        }

        private static DashboardWindowComparisonView BuildWindowComparison(
            IReadOnlyList<CityListItemView> source,
            Func<CityListItemView, DateTimeOffset?> selectMoment,
            DateTimeOffset currentStart,
            DateTimeOffset currentEnd,
            DateTimeOffset previousStart,
            DateTimeOffset previousEnd)
        {
            int current = source.Count(city => IsInsideWindow(
                moment: selectMoment(city),
                startInclusive: currentStart,
                endExclusive: currentEnd));
            int previous = source.Count(city => IsInsideWindow(
                moment: selectMoment(city),
                startInclusive: previousStart,
                endExclusive: previousEnd));

            return new DashboardWindowComparisonView(
                Current: current,
                Previous: previous,
                Delta: current - previous);
        }

        private static bool IsInsideWindow(
            DateTimeOffset? moment,
            DateTimeOffset startInclusive,
            DateTimeOffset endExclusive)
        {
            return moment is
                       { } value &&
                   value >= startInclusive &&
                   value < endExclusive;
        }

        private static bool WasReadyAt(
            CityListItemView city,
            DateTimeOffset cutoff)
        {
            return city.CreatedAtUtc <= cutoff &&
                   city.PopulationBootstrapCompletedAtUtc is
                       { } completedAtUtc &&
                   completedAtUtc <= cutoff &&
                   (city.ArchivedAtUtc is null || city.ArchivedAtUtc > cutoff);
        }

        private static bool IsReady(CityListItemView city)
        {
            return city.ArchivedAtUtc is null &&
                   city.Status.Equals(
                       value: "Active",
                       comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsArchived(CityListItemView city)
        {
            return city.ArchivedAtUtc is not null ||
                   city.Status.Equals(
                       value: "Archived",
                       comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static int GetAttentionRank(CityListItemView city)
        {
            return city.Status.ToLowerInvariant() switch
            {
                "provisioningfailed" => 0,
                "provisioning" => 1,
                _ => 2
            };
        }

    }
}

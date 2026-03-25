using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    public sealed class CityOperationsDashboardService(
        ICitiesApiClient citiesClient,
        IEnvironmentalConditionsApiClient environmentalConditionsClient,
        HealthCheckService healthCheckService,
        IHttpClientFactory httpClientFactory,
        IOptions<DownstreamServicesOptions> downstreamOptions,
        ILogger<CityOperationsDashboardService> logger) : ICityOperationsDashboardService
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly IEnvironmentalConditionsApiClient _environmentalConditionsClient = environmentalConditionsClient;
        private readonly DownstreamServicesOptions _downstreamOptions = downstreamOptions.Value;
        private readonly HealthCheckService _healthCheckService = healthCheckService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<CityOperationsDashboardService> _logger = logger;

        public async Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken)
        {
            Task<IReadOnlyList<CityListItemView>> allCitiesTask = _citiesClient.ListCitiesAsync(
                includeArchived: true,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<CityListItemView>> provisioningTask = _citiesClient.ListProvisioningCitiesAsync(
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<DashboardServiceHealthView>> healthTask = ProbeSystemHealthAsync(cancellationToken);

            await Task.WhenAll(
                allCitiesTask,
                provisioningTask,
                healthTask);

            IReadOnlyList<CityListItemView> allCities = allCitiesTask.Result;
            IReadOnlyList<CityListItemView> attentionCities = provisioningTask.Result;
            DashboardEnvironmentalAlertView[] environmentalAlerts =
                await BuildEnvironmentalAlertsAsync(
                    allCities: allCities,
                    cancellationToken: cancellationToken);
            DateTimeOffset now = DateTimeOffset.Now;

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
                GeneratedAtUtc: now,
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
                    "Ready classic-city simulations currently showing flooding, snow pressure, or degraded road access.",
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null,
                    DeltaMode: "live"),
                NewCities: BuildPeriodComparisonRow(
                    label: "New cities",
                    description: "Fresh hosts entering the system through the setup and provisioning pipeline.",
                    selectMoment: city => city.CreatedAtUtc,
                    now: now,
                    source: allCities),
                ArchivedCities: BuildPeriodComparisonRow(
                    label: "Archived cities",
                    description: "Hosts moved out of active monitoring and kept only as records.",
                    selectMoment: city => city.ArchivedAtUtc,
                    now: now,
                    source: allCities),
                FailedBootstraps: BuildPeriodComparisonRow(
                    label: "Failed bootstraps",
                    description: "Population bootstrap failures that interrupted a city before it became ready.",
                    selectMoment: city => city.PopulationBootstrapFailedAtUtc,
                    now: now,
                    source: allCities),
                ReadyHandOffs: BuildPeriodComparisonRow(
                    label: "Ready handoffs",
                    description: "Cities that completed provisioning and became available for monitoring.",
                    selectMoment: city => city.PopulationBootstrapCompletedAtUtc,
                    now: now,
                    source: allCities),
                Services: healthTask.Result,
                Events: BuildRecentEvents(allCities),
                EnvironmentalCities: environmentalAlerts.Take(8)
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
            DateTimeOffset now = DateTimeOffset.Now;
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

        private async Task<IReadOnlyList<DashboardServiceHealthView>> ProbeSystemHealthAsync(
            CancellationToken cancellationToken)
        {
            Task<DashboardServiceHealthView> gatewayTask = ProbeGatewayHealthAsync(cancellationToken);
            Task<DashboardServiceHealthView> simulationCoreTask = ProbeRemoteHealthAsync(
                service: "SimulationCore",
                baseUrl: _downstreamOptions.SimulationCore,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> simulationSystemsTask = ProbeRemoteHealthAsync(
                service: "SimulationSystems",
                baseUrl: _downstreamOptions.SimulationSystems,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> populationTask = ProbeRemoteHealthAsync(
                service: "Population",
                baseUrl: _downstreamOptions.Population,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> economyTask = ProbeRemoteHealthAsync(
                service: "Economy",
                baseUrl: _downstreamOptions.Economy,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> identityTask = ProbeRemoteHealthAsync(
                service: "Identity",
                baseUrl: _downstreamOptions.Identity,
                cancellationToken: cancellationToken);

            return await Task.WhenAll(
                gatewayTask,
                simulationCoreTask,
                simulationSystemsTask,
                populationTask,
                economyTask,
                identityTask);
        }

        private async Task<DashboardEnvironmentalAlertView[]> BuildEnvironmentalAlertsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken)
        {
            CityListItemView[] readyClassicCities = allCities
               .Where(city => IsReady(city) && IsClassicCity(city))
               .ToArray();

            if (readyClassicCities.Length == 0)
                return [];

            Task<DashboardEnvironmentalAlertView?>[] alertTasks = readyClassicCities
               .Select(city => BuildEnvironmentalAlertAsync(
                    city: city,
                    cancellationToken: cancellationToken))
               .ToArray();

            DashboardEnvironmentalAlertView?[] alerts = await Task.WhenAll(alertTasks);

            return alerts
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => alert.AlertScore)
               .ThenBy(
                    keySelector: alert => alert.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private async Task<DashboardEnvironmentalAlertView?> BuildEnvironmentalAlertAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                CityEnvironmentalConditionsView? conditions =
                    await _environmentalConditionsClient.GetCityEnvironmentalConditionsAsync(
                        cityId: city.CityId,
                        cancellationToken: cancellationToken);

                if (conditions is null)
                    return null;

                decimal alertScore = CalculateEnvironmentalAlertScore(conditions);

                if (alertScore < 0.1800m)
                    return null;

                return new DashboardEnvironmentalAlertView(
                    CityId: city.CityId,
                    CityName: city.Name,
                    CityStatus: city.Status,
                    Severity: GetEnvironmentalSeverity(alertScore),
                    Summary: BuildEnvironmentalSummary(conditions),
                    AlertScore: alertScore,
                    Conditions: conditions);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Failed to attach simulation systems metrics to city operations dashboard for cityId={CityId}.",
                    city.CityId);

                return null;
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Skipped simulation systems metrics for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                    (int)exception.StatusCode,
                    city.CityId);

                return null;
            }
        }

        private async Task<DashboardServiceHealthView> ProbeGatewayHealthAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset checkedAt = DateTimeOffset.UtcNow;

            try
            {
                HealthReport report = await _healthCheckService.CheckHealthAsync(
                    predicate: registration => registration.Tags.Contains("ready"),
                    cancellationToken: cancellationToken);

                if (report.Status == HealthStatus.Healthy)
                    return new DashboardServiceHealthView(
                        Service: "Gateway",
                        Status: "healthy",
                        Detail: "Ready endpoint is healthy.",
                        CheckedAtUtc: checkedAt);

                string detail = report.Entries.Count == 0
                    ? "Gateway ready checks reported a non-healthy state."
                    : string.Join(
                        separator: " | ",
                        values: report.Entries.Select(entry =>
                            $"{entry.Key}: {(string.IsNullOrWhiteSpace(entry.Value.Description) ? entry.Value.Status.ToString() : entry.Value.Description)}"));

                return new DashboardServiceHealthView(
                    Service: "Gateway",
                    Status: report.Status == HealthStatus.Degraded
                        ? "degraded"
                        : "unhealthy",
                    Detail: detail,
                    CheckedAtUtc: checkedAt);
            }
            catch (Exception exception)
            {
                return new DashboardServiceHealthView(
                    Service: "Gateway",
                    Status: "unhealthy",
                    Detail: $"Gateway health probe failed: {exception.GetType().Name}.",
                    CheckedAtUtc: checkedAt);
            }
        }

        private async Task<DashboardServiceHealthView> ProbeRemoteHealthAsync(
            string service,
            string baseUrl,
            CancellationToken cancellationToken)
        {
            DateTimeOffset checkedAt = DateTimeOffset.UtcNow;

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                HttpClient client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(
                    uriString: baseUrl,
                    uriKind: UriKind.Absolute);

                using HttpResponseMessage response = await client.GetAsync(
                    requestUri: "/health/ready",
                    cancellationToken: timeoutCts.Token);

                return response.IsSuccessStatusCode
                    ? new DashboardServiceHealthView(
                        Service: service,
                        Status: "healthy",
                        Detail: "Ready endpoint responded successfully.",
                        CheckedAtUtc: checkedAt)
                    : new DashboardServiceHealthView(
                        Service: service,
                        Status: "unhealthy",
                        Detail: $"Ready endpoint responded with {(int)response.StatusCode} {response.StatusCode}.",
                        CheckedAtUtc: checkedAt);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new DashboardServiceHealthView(
                    Service: service,
                    Status: "degraded",
                    Detail: "Ready probe timed out.",
                    CheckedAtUtc: checkedAt);
            }
            catch (Exception exception)
            {
                return new DashboardServiceHealthView(
                    Service: service,
                    Status: "unhealthy",
                    Detail: $"Ready probe failed: {exception.GetType().Name}.",
                    CheckedAtUtc: checkedAt);
            }
        }

        private static IReadOnlyList<DashboardRecentEventView> BuildRecentEvents(IReadOnlyList<CityListItemView> cities)
        {
            var events = new List<DashboardRecentEventView>(capacity: cities.Count * 4);

            foreach (CityListItemView city in cities)
            {
                events.Add(
                    new DashboardRecentEventView(
                        Kind: "city-created",
                        Severity: "info",
                        Title: "City created",
                        Detail: $"{city.Name} entered provisioning.",
                        CityId: city.CityId,
                        CityName: city.Name,
                        CityStatus: city.Status,
                        OccurredAtUtc: city.CreatedAtUtc));

                if (city.PopulationBootstrapCompletedAtUtc is
                    { } completedAtUtc)
                    events.Add(
                        new DashboardRecentEventView(
                            Kind: "bootstrap-ready",
                            Severity: "success",
                            Title: "Provisioning completed",
                            Detail: $"{city.Name} is ready for monitoring.",
                            CityId: city.CityId,
                            CityName: city.Name,
                            CityStatus: city.Status,
                            OccurredAtUtc: completedAtUtc));

                if (city.PopulationBootstrapFailedAtUtc is
                    { } failedAtUtc)
                {
                    string failureDetail = string.IsNullOrWhiteSpace(city.PopulationBootstrapFailureCode)
                        ? "Population bootstrap failed before the city became ready."
                        : city.PopulationBootstrapFailureCode!;

                    events.Add(
                        new DashboardRecentEventView(
                            Kind: "bootstrap-failed",
                            Severity: "danger",
                            Title: "Provisioning failed",
                            Detail: failureDetail,
                            CityId: city.CityId,
                            CityName: city.Name,
                            CityStatus: city.Status,
                            OccurredAtUtc: failedAtUtc));
                }

                if (city.ArchivedAtUtc is
                    { } archivedAtUtc)
                    events.Add(
                        new DashboardRecentEventView(
                            Kind: "city-archived",
                            Severity: "warning",
                            Title: "City archived",
                            Detail: $"{city.Name} moved out of active monitoring.",
                            CityId: city.CityId,
                            CityName: city.Name,
                            CityStatus: city.Status,
                            OccurredAtUtc: archivedAtUtc));
            }

            return events
               .OrderByDescending(@event => @event.OccurredAtUtc)
               .ThenBy(
                    keySelector: @event => @event.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .Take(10)
               .ToArray();
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

        private static bool IsClassicCity(CityListItemView city)
        {
            return city.SimulationKind.Equals(
                value: "ClassicCity",
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static decimal CalculateEnvironmentalAlertScore(CityEnvironmentalConditionsView conditions)
        {
            decimal floodingPressure = conditions.FloodingIndex;
            decimal snowPressure = conditions.SnowAccumulationIndex;
            decimal roadDisruption = 1m - conditions.RoadAccessibilityIndex;
            decimal heatingDisruption = 1m - conditions.HeatingCoverageIndex;
            decimal failureRisk = Math.Max(
                val1: conditions.Drainage.FailureRiskIndex,
                val2: Math.Max(
                    val1: conditions.SnowRemoval.FailureRiskIndex,
                    val2: Math.Max(
                        val1: conditions.RoadAccess.FailureRiskIndex,
                        val2: conditions.Heating.FailureRiskIndex)));
            decimal maintenanceBacklog = Math.Max(
                val1: conditions.Drainage.BacklogIndex,
                val2: Math.Max(
                    val1: conditions.SnowRemoval.BacklogIndex,
                    val2: Math.Max(
                        val1: conditions.RoadAccess.BacklogIndex,
                        val2: conditions.Heating.BacklogIndex)));

            decimal composite = (floodingPressure * 0.35m) +
                                (snowPressure * 0.25m) +
                                (roadDisruption * 0.20m) +
                                (heatingDisruption * 0.10m) +
                                (failureRisk * 0.07m) +
                                (maintenanceBacklog * 0.03m);

            return decimal.Round(
                d: ClampUnit(composite),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static string GetEnvironmentalSeverity(decimal alertScore)
        {
            return alertScore switch
            {
                >= 0.6500m => "danger",
                >= 0.4000m => "warning",
                _ => "info"
            };
        }

        private static string BuildEnvironmentalSummary(CityEnvironmentalConditionsView conditions)
        {
            decimal floodingPressure = conditions.FloodingIndex;
            decimal snowPressure = conditions.SnowAccumulationIndex;
            decimal roadDisruption = 1m - conditions.RoadAccessibilityIndex;
            decimal heatingDisruption = 1m - conditions.HeatingCoverageIndex;
            decimal drainagePressure = Math.Max(
                val1: conditions.Drainage.BacklogIndex,
                val2: conditions.Drainage.FailureRiskIndex);
            decimal snowRemovalPressure = Math.Max(
                val1: conditions.SnowRemoval.BacklogIndex,
                val2: conditions.SnowRemoval.FailureRiskIndex);
            decimal roadSupportPressure = Math.Max(
                val1: conditions.RoadAccess.BacklogIndex,
                val2: conditions.RoadAccess.FailureRiskIndex);
            decimal heatingPressure = Math.Max(
                val1: conditions.Heating.BacklogIndex,
                val2: conditions.Heating.FailureRiskIndex);

            if (floodingPressure >= snowPressure &&
                floodingPressure >= roadDisruption &&
                floodingPressure >= heatingDisruption &&
                floodingPressure >= drainagePressure &&
                floodingPressure >= snowRemovalPressure &&
                floodingPressure >= roadSupportPressure &&
                floodingPressure >= heatingPressure)
                return "Flooding pressure is climbing and drainage capacity is starting to stretch.";

            if (snowPressure >= roadDisruption &&
                snowPressure >= heatingDisruption &&
                snowPressure >= drainagePressure &&
                snowPressure >= snowRemovalPressure &&
                snowPressure >= roadSupportPressure &&
                snowPressure >= heatingPressure)
                return "Snow accumulation is rising and cleanup throughput is falling behind.";

            if (roadDisruption >= heatingDisruption &&
                roadDisruption >= drainagePressure &&
                roadDisruption >= snowRemovalPressure &&
                roadDisruption >= roadSupportPressure &&
                roadDisruption >= heatingPressure)
                return "Road accessibility is slipping as weather pressure reaches transport routes.";

            if (heatingDisruption >= drainagePressure &&
                heatingDisruption >= snowRemovalPressure &&
                heatingDisruption >= roadSupportPressure &&
                heatingDisruption >= heatingPressure)
                return "Heating coverage is slipping and cold-weather strain is spreading through the city.";

            if (drainagePressure >= snowRemovalPressure &&
                drainagePressure >= roadSupportPressure &&
                drainagePressure >= heatingPressure)
                return "Drainage backlog is building up and raises flood recovery risk.";

            if (snowRemovalPressure >= roadSupportPressure &&
                snowRemovalPressure >= heatingPressure)
                return "Snow-removal backlog is building up and keeps snow pressure elevated.";

            if (roadSupportPressure >= heatingPressure)
                return "Road access maintenance pressure is rising and threatens city mobility.";

            return "Heating maintenance pressure is rising and threatens stable winter coverage.";
        }

        private static decimal ClampUnit(decimal value)
        {
            return Math.Min(
                val1: 1m,
                val2: Math.Max(
                    val1: 0m,
                    val2: value));
        }
    }
}

using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    public sealed class CityOperationsDashboardService(
        ICitiesApiClient citiesClient,
        IEconomyApiClient economyClient,
        IEnvironmentalConditionsApiClient environmentalConditionsClient,
        HealthCheckService healthCheckService,
        IHttpClientFactory httpClientFactory,
        IOptions<DownstreamServicesOptions> downstreamOptions,
        ILogger<CityOperationsDashboardService> logger) : ICityOperationsDashboardService
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly IEconomyApiClient _economyClient = economyClient;
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
            DashboardBudgetPressureView[] budgetAlerts = await BuildBudgetPressureAlertsAsync(
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
                    "Ready classic-city simulations currently showing flooding, snow pressure, utility disruption, power loss, degraded road access, or supply-chain stress.",
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
                BudgetPressureCities: budgetAlerts.Take(8)
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

        private async Task<DashboardBudgetPressureView[]> BuildBudgetPressureAlertsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken)
        {
            CityListItemView[] readyClassicCities = allCities
               .Where(city => IsReady(city) && IsClassicCity(city))
               .ToArray();

            if (readyClassicCities.Length == 0)
                return [];

            Task<DashboardBudgetPressureView?>[] alertTasks = readyClassicCities
               .Select(city => BuildBudgetPressureAlertAsync(
                    city: city,
                    cancellationToken: cancellationToken))
               .ToArray();

            DashboardBudgetPressureView?[] alerts = await Task.WhenAll(alertTasks);

            return alerts
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => GetBudgetSeverityRank(alert.Severity))
               .ThenByDescending(alert => GetBudgetControlStatusRank(alert.ControlStatus))
               .ThenByDescending(alert => alert.PressureIndex)
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

        private async Task<DashboardBudgetPressureView?> BuildBudgetPressureAlertAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                CityOperationalBudgetPressureView? pressure =
                    await _economyClient.GetCityOperationalBudgetPressureAsync(
                        cityId: city.CityId,
                        cancellationToken: cancellationToken);

                if (pressure is null || !ShouldIncludeBudgetAlert(pressure))
                    return null;

                return new DashboardBudgetPressureView(
                    CityId: city.CityId,
                    CityName: city.Name,
                    CityStatus: city.Status,
                    Severity: GetBudgetSeverity(pressure),
                    Summary: BuildBudgetSummary(pressure),
                    ControlStatus: GetBudgetControlStatus(pressure),
                    PressureIndex: pressure.PressureIndex,
                    Controls: BuildBudgetControlView(pressure),
                    Budget: pressure);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Failed to attach economy operational pressure to city operations dashboard for cityId={CityId}.",
                    city.CityId);

                return null;
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Skipped economy operational pressure for city operations dashboard because Economy returned status {StatusCode} for cityId={CityId}.",
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
            decimal powerDisruption = 1m - conditions.PowerCoverageIndex;
            decimal utilityDisruption = 1m - conditions.UtilityContinuityIndex;
            decimal heatingDisruption = 1m - conditions.HeatingCoverageIndex;
            decimal waterDisruption = 1m - conditions.WaterCoverageIndex;
            decimal sanitationDisruption = 1m - conditions.SanitationCoverageIndex;
            decimal failureRisk = Max(
                conditions.Drainage.FailureRiskIndex,
                conditions.SnowRemoval.FailureRiskIndex,
                conditions.RoadAccess.FailureRiskIndex,
                conditions.PowerDistribution.FailureRiskIndex,
                conditions.UtilityIncidents.FailureRiskIndex,
                conditions.Heating.FailureRiskIndex,
                conditions.WaterDistribution.FailureRiskIndex,
                conditions.Sanitation.FailureRiskIndex);
            decimal maintenanceBacklog = Max(
                conditions.Drainage.BacklogIndex,
                conditions.SnowRemoval.BacklogIndex,
                conditions.RoadAccess.BacklogIndex,
                conditions.PowerDistribution.BacklogIndex,
                conditions.UtilityIncidents.BacklogIndex,
                conditions.Heating.BacklogIndex,
                conditions.WaterDistribution.BacklogIndex,
                conditions.Sanitation.BacklogIndex);
            decimal resourceSupplyStress = conditions.ResourceSupply.SupplyStressIndex;
            decimal resourceShortageRisk = Max(
                conditions.ResourceSupply.Fuel.ShortageRiskIndex,
                conditions.ResourceSupply.SpareParts.ShortageRiskIndex,
                conditions.ResourceSupply.Filters.ShortageRiskIndex,
                conditions.ResourceSupply.EmergencyWater.ShortageRiskIndex);

            decimal composite = (floodingPressure * 0.17m) +
                                (snowPressure * 0.11m) +
                                (roadDisruption * 0.09m) +
                                (powerDisruption * 0.09m) +
                                (utilityDisruption * 0.09m) +
                                (heatingDisruption * 0.08m) +
                                (waterDisruption * 0.08m) +
                                (sanitationDisruption * 0.06m) +
                                (failureRisk * 0.04m) +
                                (maintenanceBacklog * 0.02m) +
                                (resourceSupplyStress * 0.09m) +
                                (resourceShortageRisk * 0.08m);

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

        private static string GetBudgetSeverity(CityOperationalBudgetPressureView pressure)
        {
            int restrictionRank = Max(
                GetBudgetAuthorizationRank(pressure.GeneralAuthorizationLevel),
                GetBudgetAuthorizationRank(pressure.OperationsAuthorizationLevel),
                GetBudgetAuthorizationRank(pressure.InfrastructureAuthorizationLevel),
                GetBudgetAuthorizationRank(pressure.HealthcareAuthorizationLevel));

            if (pressure.Balance < 0m || restrictionRank >= 3 || pressure.PressureIndex >= 0.6500m)
                return "danger";

            if (restrictionRank >= 2 || pressure.PressureIndex >= 0.4000m)
                return "warning";

            return "info";
        }

        private static int GetBudgetSeverityRank(string severity)
        {
            return severity switch
            {
                "danger" => 2,
                "warning" => 1,
                _ => 0
            };
        }

        private static string BuildEnvironmentalSummary(CityEnvironmentalConditionsView conditions)
        {
            decimal floodingPressure = conditions.FloodingIndex;
            decimal snowPressure = conditions.SnowAccumulationIndex;
            decimal roadDisruption = 1m - conditions.RoadAccessibilityIndex;
            decimal powerDisruption = 1m - conditions.PowerCoverageIndex;
            decimal utilityDisruption = 1m - conditions.UtilityContinuityIndex;
            decimal heatingDisruption = 1m - conditions.HeatingCoverageIndex;
            decimal waterDisruption = 1m - conditions.WaterCoverageIndex;
            decimal sanitationDisruption = 1m - conditions.SanitationCoverageIndex;
            decimal drainagePressure = Math.Max(
                val1: conditions.Drainage.BacklogIndex,
                val2: conditions.Drainage.FailureRiskIndex);
            decimal snowRemovalPressure = Math.Max(
                val1: conditions.SnowRemoval.BacklogIndex,
                val2: conditions.SnowRemoval.FailureRiskIndex);
            decimal roadSupportPressure = Math.Max(
                val1: conditions.RoadAccess.BacklogIndex,
                val2: conditions.RoadAccess.FailureRiskIndex);
            decimal powerPressure = Math.Max(
                val1: conditions.PowerDistribution.BacklogIndex,
                val2: conditions.PowerDistribution.FailureRiskIndex);
            decimal utilityPressure = Math.Max(
                val1: conditions.UtilityIncidents.BacklogIndex,
                val2: conditions.UtilityIncidents.FailureRiskIndex);
            decimal heatingPressure = Math.Max(
                val1: conditions.Heating.BacklogIndex,
                val2: conditions.Heating.FailureRiskIndex);
            decimal waterPressure = Math.Max(
                val1: conditions.WaterDistribution.BacklogIndex,
                val2: conditions.WaterDistribution.FailureRiskIndex);
            decimal sanitationPressure = Math.Max(
                val1: conditions.Sanitation.BacklogIndex,
                val2: conditions.Sanitation.FailureRiskIndex);
            decimal resourcePressure = GetResourceSupplyPressure(conditions);
            decimal dominantPressure = Max(
                resourcePressure,
                floodingPressure,
                snowPressure,
                roadDisruption,
                powerDisruption,
                utilityDisruption,
                heatingDisruption,
                waterDisruption,
                sanitationDisruption,
                drainagePressure,
                snowRemovalPressure,
                roadSupportPressure,
                powerPressure,
                utilityPressure,
                heatingPressure,
                waterPressure,
                sanitationPressure);

            if (resourcePressure >= dominantPressure)
                return BuildResourceSupplySummary(conditions);

            if (floodingPressure >= dominantPressure)
                return "Flooding pressure is climbing and drainage capacity is starting to stretch.";

            if (snowPressure >= dominantPressure)
                return "Snow accumulation is rising and cleanup throughput is falling behind.";

            if (roadDisruption >= dominantPressure)
                return "Road accessibility is slipping as weather pressure reaches transport routes.";

            if (powerDisruption >= dominantPressure)
                return "Power coverage is slipping and substation resilience is starting to fragment across the city.";

            if (utilityDisruption >= dominantPressure)
                return "Utility restoration continuity is slipping and incident queues are starting to cascade across the city.";

            if (heatingDisruption >= dominantPressure)
                return "Heating coverage is slipping and cold-weather strain is spreading through the city.";

            if (waterDisruption >= dominantPressure)
                return "Water distribution coverage is slipping and supply reliability is starting to fragment.";

            if (sanitationDisruption >= dominantPressure)
                return "Sanitation coverage is slipping and overflow pressure is starting to spread through the city.";

            if (drainagePressure >= dominantPressure)
                return "Drainage backlog is building up and raises flood recovery risk.";

            if (snowRemovalPressure >= dominantPressure)
                return "Snow-removal backlog is building up and keeps snow pressure elevated.";

            if (roadSupportPressure >= dominantPressure)
                return "Road access maintenance pressure is rising and threatens city mobility.";

            if (powerPressure >= dominantPressure)
                return "Power-distribution maintenance pressure is rising and threatens stable citywide supply.";

            if (utilityPressure >= dominantPressure)
                return "Utility incident response pressure is rising and restoration queues are starting to stretch.";

            if (heatingPressure >= dominantPressure)
                return "Heating maintenance pressure is rising and threatens stable winter coverage.";

            if (waterPressure >= dominantPressure)
                return "Water distribution maintenance pressure is rising and threatens stable supply coverage.";

            return "Sanitation maintenance pressure is rising and threatens stable wastewater control.";
        }

        private static decimal GetResourceSupplyPressure(CityEnvironmentalConditionsView conditions)
        {
            return Max(
                conditions.ResourceSupply.SupplyStressIndex,
                conditions.ResourceSupply.Fuel.ShortageRiskIndex,
                conditions.ResourceSupply.SpareParts.ShortageRiskIndex,
                conditions.ResourceSupply.Filters.ShortageRiskIndex,
                conditions.ResourceSupply.EmergencyWater.ShortageRiskIndex);
        }

        private static string BuildResourceSupplySummary(CityEnvironmentalConditionsView conditions)
        {
            decimal supplyStress = conditions.ResourceSupply.SupplyStressIndex;
            decimal fuelShortage = conditions.ResourceSupply.Fuel.ShortageRiskIndex;
            decimal sparePartsShortage = conditions.ResourceSupply.SpareParts.ShortageRiskIndex;
            decimal filtersShortage = conditions.ResourceSupply.Filters.ShortageRiskIndex;
            decimal emergencyWaterShortage = conditions.ResourceSupply.EmergencyWater.ShortageRiskIndex;
            decimal dominantShortage = Max(
                supplyStress,
                fuelShortage,
                sparePartsShortage,
                filtersShortage,
                emergencyWaterShortage);

            if (fuelShortage >= dominantShortage)
                return "Fuel reserves are tightening and mobile utility response is starting to lose operating depth.";

            if (sparePartsShortage >= dominantShortage)
                return "Spare-parts shortages are slowing repairs and reducing restoration throughput across the city.";

            if (filtersShortage >= dominantShortage)
                return "Filters are running thin and treatment-dependent services are starting to lose resilience.";

            if (emergencyWaterShortage >= dominantShortage)
                return "Emergency water reserves are tightening and contingency coverage is starting to narrow.";

            return "Resource supply stress is rising and is starting to constrain citywide utility recovery.";
        }

        private static string BuildBudgetSummary(CityOperationalBudgetPressureView pressure)
        {
            if (GetBudgetAuthorizationRank(pressure.InfrastructureAuthorizationLevel) >= 2)
                return BuildBudgetControlConstraintSummary(
                    category: "Infrastructure",
                    authorizationLevel: pressure.InfrastructureAuthorizationLevel,
                    availableAmount: pressure.InfrastructureAvailableAmount,
                    fallback:
                    "Infrastructure maintenance dispatches are becoming a meaningful drag on the city budget.");

            if (GetBudgetAuthorizationRank(pressure.OperationsAuthorizationLevel) >= 2)
                return BuildBudgetControlConstraintSummary(
                    category: "Operations",
                    authorizationLevel: pressure.OperationsAuthorizationLevel,
                    availableAmount: pressure.OperationsAvailableAmount,
                    fallback:
                    "Emergency operations spending is climbing and starting to squeeze budget headroom.");

            if (GetBudgetAuthorizationRank(pressure.GeneralAuthorizationLevel) >= 2)
                return BuildBudgetControlConstraintSummary(
                    category: "General",
                    authorizationLevel: pressure.GeneralAuthorizationLevel,
                    availableAmount: pressure.GeneralAvailableAmount,
                    fallback:
                    "General city reserves are tightening and reduce municipal operating flexibility.");

            if (GetBudgetAuthorizationRank(pressure.HealthcareAuthorizationLevel) >= 2)
                return BuildBudgetControlConstraintSummary(
                    category: "Healthcare",
                    authorizationLevel: pressure.HealthcareAuthorizationLevel,
                    availableAmount: pressure.HealthcareAvailableAmount,
                    fallback:
                    "Healthcare budget headroom is tightening and leaves less room for medical support surges.");

            decimal dominantOperationsExpense = Math.Max(
                pressure.InfrastructureOperationsExpenses,
                pressure.EmergencyOperationsExpenses);

            if (pressure.Balance < 0m)
                return "City budget is already underwater while municipal operations keep consuming funds.";

            if (pressure.InfrastructureOperationsExpenses >= dominantOperationsExpense)
                return "Infrastructure maintenance dispatches are becoming a meaningful drag on the city budget.";

            if (pressure.EmergencyOperationsExpenses >= dominantOperationsExpense)
                return "Emergency operations spending is climbing and starting to squeeze budget headroom.";

            return "Municipal operations spending is rising and starting to narrow budget headroom.";
        }

        private static string BuildBudgetControlConstraintSummary(
            string category,
            string authorizationLevel,
            decimal availableAmount,
            string fallback)
        {
            string amount = decimal.Round(
                d: Math.Max(
                    val1: 0m,
                    val2: availableAmount),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero)
               .ToString("0.##");

            return authorizationLevel switch
            {
                "None" => $"{category} budget authorization is exhausted and operators are down to minimum response depth.",
                "Low" => $"{category} budget authorization is tight with only {amount} left for new operating decisions.",
                _ => fallback
            };
        }

        private static string GetBudgetControlStatus(CityOperationalBudgetPressureView pressure)
        {
            int restrictionRank = Max(
                GetBudgetAuthorizationRank(pressure.GeneralAuthorizationLevel),
                GetBudgetAuthorizationRank(pressure.OperationsAuthorizationLevel),
                GetBudgetAuthorizationRank(pressure.InfrastructureAuthorizationLevel),
                GetBudgetAuthorizationRank(pressure.HealthcareAuthorizationLevel));

            if (pressure.Balance < 0m || restrictionRank >= 3)
                return "restricted";

            if (restrictionRank >= 2)
                return "tight";

            if (restrictionRank >= 1 || pressure.PressureIndex >= 0.2200m)
                return "watch";

            return "open";
        }

        private static int GetBudgetControlStatusRank(string controlStatus)
        {
            return controlStatus switch
            {
                "restricted" => 3,
                "tight" => 2,
                "watch" => 1,
                _ => 0
            };
        }

        private static bool ShouldIncludeBudgetAlert(CityOperationalBudgetPressureView pressure)
        {
            return pressure.PressureIndex >= 0.2200m ||
                   GetBudgetAuthorizationRank(pressure.GeneralAuthorizationLevel) > 0 ||
                   GetBudgetAuthorizationRank(pressure.OperationsAuthorizationLevel) > 0 ||
                   GetBudgetAuthorizationRank(pressure.InfrastructureAuthorizationLevel) > 0 ||
                   GetBudgetAuthorizationRank(pressure.HealthcareAuthorizationLevel) > 0;
        }

        private static DashboardBudgetControlView BuildBudgetControlView(CityOperationalBudgetPressureView pressure)
        {
            return new DashboardBudgetControlView(
                General: new DashboardBudgetControlCategoryView(
                    Category: "General",
                    AuthorizationLevel: pressure.GeneralAuthorizationLevel,
                    AvailableAmount: pressure.GeneralAvailableAmount),
                Operations: new DashboardBudgetControlCategoryView(
                    Category: "Operations",
                    AuthorizationLevel: pressure.OperationsAuthorizationLevel,
                    AvailableAmount: pressure.OperationsAvailableAmount),
                Infrastructure: new DashboardBudgetControlCategoryView(
                    Category: "Infrastructure",
                    AuthorizationLevel: pressure.InfrastructureAuthorizationLevel,
                    AvailableAmount: pressure.InfrastructureAvailableAmount),
                Healthcare: new DashboardBudgetControlCategoryView(
                    Category: "Healthcare",
                    AuthorizationLevel: pressure.HealthcareAuthorizationLevel,
                    AvailableAmount: pressure.HealthcareAvailableAmount));
        }

        private static int GetBudgetAuthorizationRank(string authorizationLevel)
        {
            return authorizationLevel switch
            {
                "High" => 0,
                "Medium" => 1,
                "Low" => 2,
                "None" => 3,
                _ => 0
            };
        }

        private static int Max(params int[] values)
        {
            if (values.Length == 0)
                return 0;

            int current = values[0];

            for (int index = 1; index < values.Length; index++)
                current = Math.Max(
                    val1: current,
                    val2: values[index]);

            return current;
        }

        private static decimal Max(params decimal[] values)
        {
            if (values.Length == 0)
                return 0m;

            decimal current = values[0];

            for (int index = 1; index < values.Length; index++)
                current = Math.Max(
                    val1: current,
                    val2: values[index]);

            return current;
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

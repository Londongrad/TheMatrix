using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.CityCore.Dashboard;
using Matrix.ApiGateway.DownstreamClients.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.CityCore.Dashboard
{
    public sealed class CityOperationsDashboardService(
        ICitiesApiClient citiesClient,
        HealthCheckService healthCheckService,
        IHttpClientFactory httpClientFactory,
        IOptions<DownstreamServicesOptions> downstreamOptions) : ICityOperationsDashboardService
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly HealthCheckService _healthCheckService = healthCheckService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly DownstreamServicesOptions _downstreamOptions = downstreamOptions.Value;

        public async Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken)
        {
            Task<IReadOnlyList<CityListItemView>> allCitiesTask = _citiesClient.ListCitiesAsync(
                includeArchived: true,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<CityListItemView>> provisioningTask = _citiesClient.ListProvisioningCitiesAsync(
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<DashboardServiceHealthView>> healthTask = ProbeSystemHealthAsync(cancellationToken);

            await Task.WhenAll(allCitiesTask, provisioningTask, healthTask);

            var allCities = allCitiesTask.Result;
            var attentionCities = provisioningTask.Result;
            var now = DateTimeOffset.Now;

            var readyCities = allCities
               .Where(IsReady)
               .OrderByDescending(city => city.PopulationBootstrapCompletedAtUtc ?? city.CreatedAtUtc)
               .ThenBy(city => city.Name, StringComparer.OrdinalIgnoreCase)
               .Take(6)
               .ToArray();

            var archivedCities = allCities
               .Where(IsArchived)
               .OrderByDescending(city => city.ArchivedAtUtc ?? DateTimeOffset.MinValue)
               .ThenBy(city => city.Name, StringComparer.OrdinalIgnoreCase)
               .Take(6)
               .ToArray();

            var rankedAttentionCities = attentionCities
               .OrderBy(city => GetAttentionRank(city))
               .ThenByDescending(city => city.PopulationBootstrapFailedAtUtc ?? city.CreatedAtUtc)
               .ThenBy(city => city.Name, StringComparer.OrdinalIgnoreCase)
               .ToArray();

            return new CityOperationsDashboardView(
                GeneratedAtUtc: now,
                TrackedHosts: BuildSnapshotMetric(
                    label: "Tracked hosts",
                    description: "Total city records still visible to operators across live, provisioning, and archived workspaces.",
                    current: allCities.Count,
                    countAtCutoff: cutoff => allCities.Count(city => city.CreatedAtUtc <= cutoff)),
                ReadyHosts: BuildSnapshotMetric(
                    label: "Ready monitoring",
                    description: "Cities that have completed bootstrap and are currently live for direct monitoring.",
                    current: allCities.Count(IsReady),
                    countAtCutoff: cutoff => allCities.Count(city => WasReadyAt(city, cutoff))),
                ArchivedRecords: BuildSnapshotMetric(
                    label: "Archived records",
                    description: "Historical city records retained for audit, cleanup, and post-mortem review.",
                    current: allCities.Count(IsArchived),
                    countAtCutoff: cutoff => allCities.Count(city => city.ArchivedAtUtc is { } archivedAtUtc && archivedAtUtc <= cutoff)),
                AttentionQueue: new DashboardMetricView(
                    Label: "Attention queue",
                    Current: rankedAttentionCities.Length,
                    Description: "Provisioning handoffs or failed launches that still need active operator follow-up.",
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
                AttentionCities: rankedAttentionCities.Take(8).ToArray(),
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
            DateTimeOffset dayStart = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
            DateTimeOffset monthStart = new(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
            DateTimeOffset yearStart = new(now.Year, 1, 1, 0, 0, 0, now.Offset);

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
            DateTimeOffset dayStart = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
            DateTimeOffset monthStart = new(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
            DateTimeOffset yearStart = new(now.Year, 1, 1, 0, 0, 0, now.Offset);

            DateTimeOffset previousDayStart = dayStart.AddDays(-1);
            DateTimeOffset previousMonthStart = monthStart.AddMonths(-1);
            DateTimeOffset previousYearStart = yearStart.AddYears(-1);

            return new DashboardPeriodComparisonRowView(
                Label: label,
                Description: description,
                Yesterday: BuildWindowComparison(source, selectMoment, dayStart, now, previousDayStart, dayStart),
                Month: BuildWindowComparison(source, selectMoment, monthStart, now, previousMonthStart, monthStart),
                Year: BuildWindowComparison(source, selectMoment, yearStart, now, previousYearStart, yearStart));
        }

        private static DashboardWindowComparisonView BuildWindowComparison(
            IReadOnlyList<CityListItemView> source,
            Func<CityListItemView, DateTimeOffset?> selectMoment,
            DateTimeOffset currentStart,
            DateTimeOffset currentEnd,
            DateTimeOffset previousStart,
            DateTimeOffset previousEnd)
        {
            int current = source.Count(city => IsInsideWindow(selectMoment(city), currentStart, currentEnd));
            int previous = source.Count(city => IsInsideWindow(selectMoment(city), previousStart, previousEnd));

            return new DashboardWindowComparisonView(
                Current: current,
                Previous: previous,
                Delta: current - previous);
        }

        private async Task<IReadOnlyList<DashboardServiceHealthView>> ProbeSystemHealthAsync(CancellationToken cancellationToken)
        {
            Task<DashboardServiceHealthView> gatewayTask = ProbeGatewayHealthAsync(cancellationToken);
            Task<DashboardServiceHealthView> cityCoreTask = ProbeRemoteHealthAsync("CityCore", _downstreamOptions.CityCore, cancellationToken);
            Task<DashboardServiceHealthView> populationTask = ProbeRemoteHealthAsync("Population", _downstreamOptions.Population, cancellationToken);
            Task<DashboardServiceHealthView> identityTask = ProbeRemoteHealthAsync("Identity", _downstreamOptions.Identity, cancellationToken);

            return await Task.WhenAll(gatewayTask, cityCoreTask, populationTask, identityTask);
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
                {
                    return new DashboardServiceHealthView(
                        Service: "Gateway",
                        Status: "healthy",
                        Detail: "Ready endpoint is healthy.",
                        CheckedAtUtc: checkedAt);
                }

                string detail = report.Entries.Count == 0
                    ? "Gateway ready checks reported a non-healthy state."
                    : string.Join(
                        separator: " | ",
                        values: report.Entries.Select(entry =>
                            $"{entry.Key}: {(string.IsNullOrWhiteSpace(entry.Value.Description) ? entry.Value.Status.ToString() : entry.Value.Description)}"));

                return new DashboardServiceHealthView(
                    Service: "Gateway",
                    Status: report.Status == HealthStatus.Degraded ? "degraded" : "unhealthy",
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
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

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

                if (city.PopulationBootstrapCompletedAtUtc is { } completedAtUtc)
                {
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
                }

                if (city.PopulationBootstrapFailedAtUtc is { } failedAtUtc)
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

                if (city.ArchivedAtUtc is { } archivedAtUtc)
                {
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
            }

            return events
               .OrderByDescending(@event => @event.OccurredAtUtc)
               .ThenBy(@event => @event.CityName, StringComparer.OrdinalIgnoreCase)
               .Take(10)
               .ToArray();
        }

        private static bool IsInsideWindow(
            DateTimeOffset? moment,
            DateTimeOffset startInclusive,
            DateTimeOffset endExclusive)
        {
            return moment is { } value &&
                   value >= startInclusive &&
                   value < endExclusive;
        }

        private static bool WasReadyAt(CityListItemView city, DateTimeOffset cutoff)
        {
            return city.CreatedAtUtc <= cutoff &&
                   city.PopulationBootstrapCompletedAtUtc is { } completedAtUtc &&
                   completedAtUtc <= cutoff &&
                   (city.ArchivedAtUtc is null || city.ArchivedAtUtc > cutoff);
        }

        private static bool IsReady(CityListItemView city)
        {
            return city.ArchivedAtUtc is null &&
                   city.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsArchived(CityListItemView city)
        {
            return city.ArchivedAtUtc is not null ||
                   city.Status.Equals("Archived", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetAttentionRank(CityListItemView city)
        {
            return city.Status.ToLowerInvariant() switch
            {
                "provisioningfailed" => 0,
                "provisioning" => 1,
                _ => 2,
            };
        }
    }
}

using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    public sealed class CityOperationsDashboardService(
        ICitiesApiClient citiesClient,
        IEconomyApiClient economyClient,
        IPopulationApiClient populationClient,
        IStockpilesApiClient stockpilesClient,
        ITripsApiClient tripsClient,
        IEnvironmentalConditionsApiClient environmentalConditionsClient,
        HealthCheckService healthCheckService,
        IHttpClientFactory httpClientFactory,
        IOptions<DownstreamServicesOptions> downstreamOptions,
        ILogger<CityOperationsDashboardService> logger) : ICityOperationsDashboardService
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly IEconomyApiClient _economyClient = economyClient;
        private readonly IPopulationApiClient _populationClient = populationClient;
        private readonly IStockpilesApiClient _stockpilesClient = stockpilesClient;
        private readonly ITripsApiClient _tripsClient = tripsClient;
        private readonly IEnvironmentalConditionsApiClient _environmentalConditionsClient = environmentalConditionsClient;
        private readonly DownstreamServicesOptions _downstreamOptions = downstreamOptions.Value;
        private readonly HealthCheckService _healthCheckService = healthCheckService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<CityOperationsDashboardService> _logger = logger;

        private sealed record CityOperationalSnapshot(
            CityListItemView City,
            CityEnvironmentalConditionsView? Conditions,
            CityPopulationDistrictPressureDto? PopulationDistrictPressure,
            CityDistrictHeatingConditionsView? DistrictHeating,
            CityDistrictWaterDistributionConditionsView? DistrictWater,
            CityDistrictPowerDistributionConditionsView? DistrictPower,
            CityDistrictSanitationConditionsView? DistrictSanitation,
            CityDistrictUtilityIncidentConditionsView? DistrictUtilityIncidents,
            IReadOnlyList<CityActiveTripView>? ActiveTrips,
            CityStockpilesView? Stockpiles,
            CityOperationalBudgetPressureView? Budget);

        private sealed record ServicePhaseState(
            string Service,
            long TickId,
            string Phase,
            int PhaseRank);

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

            IReadOnlyList<CityListItemView> allCities = await allCitiesTask;
            IReadOnlyList<CityListItemView> attentionCities = await provisioningTask;
            IReadOnlyList<DashboardServiceHealthView> services = await healthTask;
            IReadOnlyList<CityOperationalSnapshot> operationalSnapshots =
                await LoadReadyClassicCitySnapshotsAsync(
                    allCities: allCities,
                    cancellationToken: cancellationToken);
            DashboardEnvironmentalAlertView[] environmentalAlerts = BuildEnvironmentalAlerts(operationalSnapshots);
            DashboardPopulationDistrictPressureView[] populationDistrictAlerts = BuildPopulationDistrictPressureAlerts(operationalSnapshots);
            DashboardDistrictResponsePriorityView[] districtResponsePriorities = BuildDistrictResponsePriorities(operationalSnapshots);
            DashboardMobilityView[] mobilityAlerts = BuildMobilityAlerts(operationalSnapshots);
            DashboardBudgetPressureView[] budgetAlerts = BuildBudgetPressureAlerts(operationalSnapshots);
            DashboardTickFreshnessView[] tickFreshnessAlerts = BuildTickFreshnessAlerts(operationalSnapshots);
            DashboardPhaseProgressView[] phaseProgressAlerts = BuildPhaseProgressAlerts(operationalSnapshots);
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
                Services: services,
                Events: BuildRecentEvents(allCities),
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
            Task<DashboardServiceHealthView> resourcesTask = ProbeRemoteHealthAsync(
                service: "Resources",
                baseUrl: _downstreamOptions.Resources,
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
                resourcesTask,
                populationTask,
                economyTask,
                identityTask);
        }

        private async Task<IReadOnlyList<CityOperationalSnapshot>> LoadReadyClassicCitySnapshotsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken)
        {
            CityListItemView[] readyClassicCities = allCities
               .Where(city => IsReady(city) && IsClassicCity(city))
               .ToArray();

            if (readyClassicCities.Length == 0)
                return [];

            Task<CityOperationalSnapshot>[] snapshotTasks = readyClassicCities
               .Select(city => LoadReadyClassicCitySnapshotAsync(
                    city: city,
                    cancellationToken: cancellationToken))
               .ToArray();

            return await Task.WhenAll(snapshotTasks);
        }

        private async Task<CityOperationalSnapshot> LoadReadyClassicCitySnapshotAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            Task<CityEnvironmentalConditionsView?> environmentalTask = TryLoadEnvironmentalConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityPopulationDistrictPressureDto?> populationDistrictPressureTask = TryLoadPopulationDistrictPressureAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityDistrictHeatingConditionsView?> districtHeatingTask = TryLoadDistrictHeatingConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityDistrictWaterDistributionConditionsView?> districtWaterTask = TryLoadDistrictWaterConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityDistrictPowerDistributionConditionsView?> districtPowerTask = TryLoadDistrictPowerConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityDistrictSanitationConditionsView?> districtSanitationTask = TryLoadDistrictSanitationConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityDistrictUtilityIncidentConditionsView?> districtUtilityIncidentsTask = TryLoadDistrictUtilityIncidentConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<CityActiveTripView>?> activeTripsTask = TryLoadActiveTripsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityStockpilesView?> stockpilesTask = TryLoadStockpilesAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityOperationalBudgetPressureView?> budgetTask = TryLoadBudgetPressureAsync(
                city: city,
                cancellationToken: cancellationToken);

            await Task.WhenAll(
                environmentalTask,
                populationDistrictPressureTask,
                districtHeatingTask,
                districtWaterTask,
                districtPowerTask,
                districtSanitationTask,
                districtUtilityIncidentsTask,
                activeTripsTask,
                stockpilesTask,
                budgetTask);

            CityEnvironmentalConditionsView? environmental = await environmentalTask;
            CityPopulationDistrictPressureDto? populationDistrictPressure = await populationDistrictPressureTask;
            CityDistrictHeatingConditionsView? districtHeating = await districtHeatingTask;
            CityDistrictWaterDistributionConditionsView? districtWater = await districtWaterTask;
            CityDistrictPowerDistributionConditionsView? districtPower = await districtPowerTask;
            CityDistrictSanitationConditionsView? districtSanitation = await districtSanitationTask;
            CityDistrictUtilityIncidentConditionsView? districtUtilityIncidents = await districtUtilityIncidentsTask;
            IReadOnlyList<CityActiveTripView>? activeTrips = await activeTripsTask;
            CityStockpilesView? stockpiles = await stockpilesTask;
            CityOperationalBudgetPressureView? budget = await budgetTask;

            return new CityOperationalSnapshot(
                City: city,
                Conditions: environmental,
                PopulationDistrictPressure: populationDistrictPressure,
                DistrictHeating: districtHeating,
                DistrictWater: districtWater,
                DistrictPower: districtPower,
                DistrictSanitation: districtSanitation,
                DistrictUtilityIncidents: districtUtilityIncidents,
                ActiveTrips: activeTrips,
                Stockpiles: stockpiles,
                Budget: budget);
        }

        private static DashboardEnvironmentalAlertView[] BuildEnvironmentalAlerts(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildEnvironmentalAlert)
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => alert.AlertScore)
               .ThenBy(
                    keySelector: alert => alert.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private static DashboardPopulationDistrictPressureView[] BuildPopulationDistrictPressureAlerts(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildPopulationDistrictPressureAlert)
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => GetPopulationDistrictSeverityRank(alert.Severity))
               .ThenByDescending(alert => alert.PopulationPressureIndex)
               .ThenByDescending(alert => alert.HomelessResidentCount)
               .ThenByDescending(alert => alert.ActiveIllnessCount)
               .ThenBy(
                    keySelector: alert => alert.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private static DashboardDistrictResponsePriorityView[] BuildDistrictResponsePriorities(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildDistrictResponsePriority)
               .Where(priority => priority is not null)
               .Select(priority => priority!)
               .OrderByDescending(priority => GetDistrictResponseSeverityRank(priority.Severity))
               .ThenByDescending(priority => priority.PriorityScore)
               .ThenBy(
                    keySelector: priority => priority.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private static DashboardMobilityView[] BuildMobilityAlerts(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildMobilityAlert)
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => GetMobilitySeverityRank(alert.Severity))
               .ThenByDescending(alert => alert.MobilityPressureIndex)
               .ThenByDescending(alert => alert.ActiveHealthcareTripCount)
               .ThenByDescending(alert => alert.ActiveTripCount)
               .ThenBy(
                    keySelector: alert => alert.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private static DashboardBudgetPressureView[] BuildBudgetPressureAlerts(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildBudgetPressureAlert)
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

        private static DashboardTickFreshnessView[] BuildTickFreshnessAlerts(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildTickFreshnessAlert)
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => GetTickFreshnessSeverityRank(alert.Severity))
               .ThenByDescending(alert => alert.TickSkew)
               .ThenBy(
                    keySelector: alert => alert.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private static DashboardPhaseProgressView[] BuildPhaseProgressAlerts(
            IReadOnlyList<CityOperationalSnapshot> snapshots)
        {
            return snapshots
               .Select(BuildPhaseProgressAlert)
               .Where(alert => alert is not null)
               .Select(alert => alert!)
               .OrderByDescending(alert => GetPhaseProgressSeverityRank(alert.Severity))
               .ThenByDescending(alert => alert.TickSpread)
               .ThenBy(
                    keySelector: alert => alert.CityName,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }

        private async Task<CityEnvironmentalConditionsView?> TryLoadEnvironmentalConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _environmentalConditionsClient.GetCityEnvironmentalConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: cancellationToken);
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

        private async Task<CityPopulationDistrictPressureDto?> TryLoadPopulationDistrictPressureAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _populationClient.GetCityDistrictPressureAsync(
                    cityId: city.CityId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Failed to attach population district pressure to city operations dashboard for cityId={CityId}.",
                    city.CityId);

                return null;
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Skipped population district pressure for city operations dashboard because Population returned status {StatusCode} for cityId={CityId}.",
                    (int)exception.StatusCode,
                    city.CityId);

                return null;
            }
        }

        private async Task<CityDistrictHeatingConditionsView?> TryLoadDistrictHeatingConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDistrictUtilityConditionsAsync(
                city: city,
                load: token => _environmentalConditionsClient.GetCityDistrictHeatingConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage: "Failed to attach district heating conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district heating conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictWaterDistributionConditionsView?> TryLoadDistrictWaterConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDistrictUtilityConditionsAsync(
                city: city,
                load: token => _environmentalConditionsClient.GetCityDistrictWaterDistributionConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage: "Failed to attach district water conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district water conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictPowerDistributionConditionsView?> TryLoadDistrictPowerConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDistrictUtilityConditionsAsync(
                city: city,
                load: token => _environmentalConditionsClient.GetCityDistrictPowerDistributionConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage: "Failed to attach district power conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district power conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictSanitationConditionsView?> TryLoadDistrictSanitationConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDistrictUtilityConditionsAsync(
                city: city,
                load: token => _environmentalConditionsClient.GetCityDistrictSanitationConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage: "Failed to attach district sanitation conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district sanitation conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictUtilityIncidentConditionsView?> TryLoadDistrictUtilityIncidentConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDistrictUtilityConditionsAsync(
                city: city,
                load: token => _environmentalConditionsClient.GetCityDistrictUtilityIncidentConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach district utility incidents to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district utility incidents for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<IReadOnlyList<CityActiveTripView>?> TryLoadActiveTripsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _tripsClient.GetActiveTripsAsync(
                    cityId: city.CityId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Failed to attach active trips to city operations dashboard for cityId={CityId}.",
                    city.CityId);

                return null;
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Skipped active trips for city operations dashboard because SimulationCore returned status {StatusCode} for cityId={CityId}.",
                    (int)exception.StatusCode,
                    city.CityId);

                return null;
            }
        }

        private async Task<T?> TryLoadDistrictUtilityConditionsAsync<T>(
            CityListItemView city,
            Func<CancellationToken, Task<T?>> load,
            string failureMessage,
            string skippedMessage,
            CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                return await load(cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message: failureMessage,
                    city.CityId);

                return null;
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message: skippedMessage,
                    (int)exception.StatusCode,
                    city.CityId);

                return null;
            }
        }

        private async Task<CityOperationalBudgetPressureView?> TryLoadBudgetPressureAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _economyClient.GetCityOperationalBudgetPressureAsync(
                    cityId: city.CityId,
                    cancellationToken: cancellationToken);
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

        private async Task<CityStockpilesView?> TryLoadStockpilesAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _stockpilesClient.GetCityStockpilesAsync(
                    cityId: city.CityId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Failed to attach resource stockpiles to city operations dashboard for cityId={CityId}.",
                    city.CityId);

                return null;
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Skipped resource stockpiles for city operations dashboard because Resources returned status {StatusCode} for cityId={CityId}.",
                    (int)exception.StatusCode,
                    city.CityId);

                return null;
            }
        }

        private static DashboardEnvironmentalAlertView? BuildEnvironmentalAlert(CityOperationalSnapshot snapshot)
        {
            CityEnvironmentalConditionsView? conditions = snapshot.Conditions;

            if (conditions is null)
                return null;

            decimal alertScore = CalculateEnvironmentalAlertScore(conditions);

            if (alertScore < 0.1800m)
                return null;

            return new DashboardEnvironmentalAlertView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                Severity: GetEnvironmentalSeverity(alertScore),
                Summary: BuildEnvironmentalSummary(conditions),
                AlertScore: alertScore,
                Conditions: conditions);
        }

        private static DashboardPopulationDistrictPressureView? BuildPopulationDistrictPressureAlert(
            CityOperationalSnapshot snapshot)
        {
            CityPopulationDistrictPressureDto? districtPressure = snapshot.PopulationDistrictPressure;

            if (districtPressure is null || districtPressure.Districts.Count == 0)
                return null;

            CityPopulationDistrictPressureItemDto leadingDistrict = districtPressure.Districts
               .OrderByDescending(x => x.PopulationPressureIndex)
               .ThenByDescending(x => x.HomelessResidentCount)
               .ThenByDescending(x => x.ActiveIllnessCount)
               .First();

            if (leadingDistrict.PopulationPressureIndex < 0.2800m)
                return null;

            return new DashboardPopulationDistrictPressureView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                DistrictId: leadingDistrict.DistrictId,
                Severity: GetPopulationDistrictSeverity(leadingDistrict.PopulationPressureIndex),
                Summary: BuildPopulationDistrictSummary(leadingDistrict),
                PopulationPressureIndex: leadingDistrict.PopulationPressureIndex,
                UtilityContinuityIndex: leadingDistrict.UtilityContinuityIndex,
                HousingFragilityIndex: leadingDistrict.HousingFragilityIndex,
                ResidentCount: leadingDistrict.ResidentCount,
                ActiveIllnessCount: leadingDistrict.ActiveIllnessCount,
                SevereIllnessCount: leadingDistrict.SevereIllnessCount,
                HomelessResidentCount: leadingDistrict.HomelessResidentCount,
                District: leadingDistrict);
        }

        private static DashboardDistrictResponsePriorityView? BuildDistrictResponsePriority(
            CityOperationalSnapshot snapshot)
        {
            CityPopulationDistrictPressureDto? districtPressure = snapshot.PopulationDistrictPressure;

            if (districtPressure is null || districtPressure.Districts.Count == 0)
                return null;

            DashboardDistrictResponsePriorityView? leadingPriority = districtPressure.Districts
               .Select(district => BuildDistrictResponsePriority(
                    snapshot: snapshot,
                    district: district))
               .Where(priority => priority is not null)
               .Select(priority => priority!)
               .OrderByDescending(priority => priority.PriorityScore)
               .ThenByDescending(priority => priority.HomelessResidentCount)
               .ThenByDescending(priority => priority.ActiveIllnessCount)
               .FirstOrDefault();

            return leadingPriority is not null && leadingPriority.PriorityScore >= 0.3400m
                ? leadingPriority
                : null;
        }

        private static DashboardDistrictResponsePriorityView? BuildDistrictResponsePriority(
            CityOperationalSnapshot snapshot,
            CityPopulationDistrictPressureItemDto district)
        {
            CityDistrictHeatingConditionView? heating = snapshot.DistrictHeating?.Districts
               .FirstOrDefault(x => x.DistrictId == district.DistrictId);
            CityDistrictWaterDistributionConditionView? water = snapshot.DistrictWater?.Districts
               .FirstOrDefault(x => x.DistrictId == district.DistrictId);
            CityDistrictPowerDistributionConditionView? power = snapshot.DistrictPower?.Districts
               .FirstOrDefault(x => x.DistrictId == district.DistrictId);
            CityDistrictSanitationConditionView? sanitation = snapshot.DistrictSanitation?.Districts
               .FirstOrDefault(x => x.DistrictId == district.DistrictId);
            CityDistrictUtilityIncidentConditionView? incidents = snapshot.DistrictUtilityIncidents?.Districts
               .FirstOrDefault(x => x.DistrictId == district.DistrictId);
            decimal utilityIncidentPressure = incidents?.IncidentPressureIndex ?? district.UtilityIncidentPressureIndex;
            decimal serviceDisruptionIndex = ClampUnit(Max(
                heating is null ? 0m : 1m - heating.HeatingCoverageIndex,
                water is null ? 0m : 1m - water.WaterCoverageIndex,
                power is null ? 0m : 1m - power.PowerCoverageIndex,
                sanitation is null ? 0m : 1m - sanitation.SanitationCoverageIndex,
                1m - ClampUnit(district.UtilityContinuityIndex)));
            decimal maintenancePriorityIndex = ClampUnit(Max(
                heating?.MaintenancePriorityIndex ?? 0m,
                water?.MaintenancePriorityIndex ?? 0m,
                power?.MaintenancePriorityIndex ?? 0m,
                sanitation?.MaintenancePriorityIndex ?? 0m,
                incidents?.RestorationPriorityIndex ?? 0m));
            decimal priorityScore = decimal.Round(
                d: ClampUnit(
                    (district.PopulationPressureIndex * 0.42m) +
                    (utilityIncidentPressure * 0.18m) +
                    (serviceDisruptionIndex * 0.22m) +
                    (maintenancePriorityIndex * 0.18m)),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            return new DashboardDistrictResponsePriorityView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                DistrictId: district.DistrictId,
                Severity: GetDistrictResponseSeverity(priorityScore),
                Summary: BuildDistrictResponseSummary(
                    district: district,
                    heating: heating,
                    water: water,
                    power: power,
                    sanitation: sanitation,
                    incidents: incidents,
                    serviceDisruptionIndex: serviceDisruptionIndex,
                    maintenancePriorityIndex: maintenancePriorityIndex),
                RecommendedFocus: BuildDistrictResponseFocus(
                    district: district,
                    heating: heating,
                    water: water,
                    power: power,
                    sanitation: sanitation,
                    incidents: incidents),
                PriorityScore: priorityScore,
                PopulationPressureIndex: district.PopulationPressureIndex,
                UtilityIncidentPressureIndex: decimal.Round(
                    d: utilityIncidentPressure,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero),
                ServiceDisruptionIndex: decimal.Round(
                    d: serviceDisruptionIndex,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero),
                MaintenancePriorityIndex: decimal.Round(
                    d: maintenancePriorityIndex,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero),
                ActiveIllnessCount: district.ActiveIllnessCount,
                SevereIllnessCount: district.SevereIllnessCount,
                HomelessResidentCount: district.HomelessResidentCount);
        }

        private static DashboardMobilityView? BuildMobilityAlert(CityOperationalSnapshot snapshot)
        {
            CityActiveTripView[] trips = snapshot.ActiveTrips?
               .Where(trip => string.Equals(
                    a: trip.Status,
                    b: "Active",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
               .ToArray() ?? [];

            if (trips.Length == 0)
                return null;

            int activeTripCount = trips.Length;
            int activeCommuteCount = trips.Count(trip => IsCommutePurpose(trip.Purpose));
            int activeHealthcareTripCount = trips.Count(trip => IsHealthcarePurpose(trip.Purpose));
            int dynamicRoadTripCount = trips.Count(trip => trip.UsedDynamicRoadConditions);
            int delayedTripCount = trips.Count(trip => GetTripSlowdownRatio(trip) >= 1.2000m);
            decimal averageSlowdownRatio = decimal.Round(
                d: trips.Average(GetTripSlowdownRatio),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            decimal averageRemainingTravelMinutes = decimal.Round(
                d: trips.Average(GetRemainingTravelMinutes),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            decimal loadIndex = ClampUnit(
                value: ((activeCommuteCount + (activeHealthcareTripCount * 1.5m)) / 18m));
            decimal healthcareLoadIndex = ClampUnit(activeHealthcareTripCount / 4m);
            decimal dynamicRoadExposure = ClampUnit(dynamicRoadTripCount / activeTripCount);
            decimal delayExposure = ClampUnit(delayedTripCount / activeTripCount);
            decimal slowdownIndex = ClampUnit((averageSlowdownRatio - 1m) / 0.75m);
            decimal remainingTravelIndex = ClampUnit(averageRemainingTravelMinutes / 120m);
            decimal mobilityPressureIndex = decimal.Round(
                d: ClampUnit(
                    value: (loadIndex * 0.30m) +
                           (healthcareLoadIndex * 0.25m) +
                           (delayExposure * 0.20m) +
                           (slowdownIndex * 0.10m) +
                           (dynamicRoadExposure * 0.10m) +
                           (remainingTravelIndex * 0.05m)),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            if (mobilityPressureIndex < 0.2200m && activeHealthcareTripCount == 0)
                return null;

            if (mobilityPressureIndex < 0.1800m)
                return null;

            return new DashboardMobilityView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                Severity: GetMobilitySeverity(mobilityPressureIndex),
                Summary: BuildMobilitySummary(
                    activeCommuteCount: activeCommuteCount,
                    activeHealthcareTripCount: activeHealthcareTripCount,
                    delayExposure: delayExposure,
                    dynamicRoadExposure: dynamicRoadExposure,
                    averageRemainingTravelMinutes: averageRemainingTravelMinutes),
                MobilityPressureIndex: mobilityPressureIndex,
                ActiveTripCount: activeTripCount,
                ActiveCommuteCount: activeCommuteCount,
                ActiveHealthcareTripCount: activeHealthcareTripCount,
                DelayedTripCount: delayedTripCount,
                DynamicRoadTripCount: dynamicRoadTripCount,
                AverageSlowdownRatio: averageSlowdownRatio,
                AverageRemainingTravelMinutes: averageRemainingTravelMinutes,
                Trips: trips);
        }

        private static DashboardBudgetPressureView? BuildBudgetPressureAlert(CityOperationalSnapshot snapshot)
        {
            CityOperationalBudgetPressureView? pressure = snapshot.Budget;

            if (pressure is null || !ShouldIncludeBudgetAlert(pressure))
                return null;

            return new DashboardBudgetPressureView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                Severity: GetBudgetSeverity(pressure),
                Summary: BuildBudgetSummary(pressure),
                ControlStatus: GetBudgetControlStatus(pressure),
                PressureIndex: pressure.PressureIndex,
                Controls: BuildBudgetControlView(pressure),
                Budget: pressure);
        }

        private static DashboardTickFreshnessView? BuildTickFreshnessAlert(CityOperationalSnapshot snapshot)
        {
            CityEnvironmentalConditionsView? conditions = snapshot.Conditions;
            CityOperationalBudgetPressureView? budget = snapshot.Budget;

            if (conditions is null || budget is null)
                return null;

            long tickSkew = Math.Abs(conditions.EffectiveTickId - budget.EffectiveTickId);

            if (tickSkew < 2)
                return null;

            return new DashboardTickFreshnessView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                Severity: GetTickFreshnessSeverity(tickSkew),
                Summary: BuildTickFreshnessSummary(
                    environmentalTickId: conditions.EffectiveTickId,
                    budgetTickId: budget.EffectiveTickId,
                    tickSkew: tickSkew),
                EnvironmentalTickId: conditions.EffectiveTickId,
                BudgetTickId: budget.EffectiveTickId,
                TickSkew: tickSkew,
                EnvironmentalEvaluatedAtUtc: conditions.LastEvaluatedAtUtc,
                BudgetEvaluatedAtUtc: budget.EffectiveAtUtc);
        }

        private static DashboardPhaseProgressView? BuildPhaseProgressAlert(CityOperationalSnapshot snapshot)
        {
            CityEnvironmentalConditionsView? conditions = snapshot.Conditions;
            CityStockpilesView? stockpiles = snapshot.Stockpiles;
            CityOperationalBudgetPressureView? budget = snapshot.Budget;

            if (conditions is null || stockpiles is null || budget is null)
                return null;

            bool orderingViolation = stockpiles.EffectiveTickId > conditions.EffectiveTickId ||
                                     budget.EffectiveTickId > stockpiles.EffectiveTickId;
            long maxTick = Max(
                conditions.EffectiveTickId,
                stockpiles.EffectiveTickId,
                budget.EffectiveTickId);
            long minTick = Min(
                conditions.EffectiveTickId,
                stockpiles.EffectiveTickId,
                budget.EffectiveTickId);
            long tickSpread = maxTick - minTick;

            if (!orderingViolation && tickSpread == 0)
                return null;

            ServicePhaseState laggingState = ResolveLaggingState(
                conditions: conditions,
                stockpiles: stockpiles,
                budget: budget);
            ServicePhaseState leadingState = ResolveLeadingState(
                conditions: conditions,
                stockpiles: stockpiles,
                budget: budget);

            return new DashboardPhaseProgressView(
                CityId: snapshot.City.CityId,
                CityName: snapshot.City.Name,
                CityStatus: snapshot.City.Status,
                Severity: GetPhaseProgressSeverity(
                    orderingViolation: orderingViolation,
                    tickSpread: tickSpread),
                Summary: BuildPhaseProgressSummary(
                    orderingViolation: orderingViolation,
                    conditions: conditions,
                    stockpiles: stockpiles,
                    budget: budget,
                    laggingState: laggingState,
                    leadingState: leadingState,
                    tickSpread: tickSpread),
                SystemsTickId: conditions.EffectiveTickId,
                SystemsPhase: conditions.EffectivePhase,
                ResourcesTickId: stockpiles.EffectiveTickId,
                ResourcesPhase: stockpiles.EffectivePhase,
                BudgetTickId: budget.EffectiveTickId,
                BudgetPhase: budget.EffectivePhase,
                TickSpread: tickSpread,
                LaggingService: laggingState.Service,
                LeadingService: leadingState.Service,
                Conditions: conditions,
                Stockpiles: stockpiles,
                Budget: budget);
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

        private static string GetMobilitySeverity(decimal mobilityPressureIndex)
        {
            return mobilityPressureIndex switch
            {
                >= 0.6800m => "danger",
                >= 0.4400m => "warning",
                _ => "info"
            };
        }

        private static string GetPopulationDistrictSeverity(decimal pressureIndex)
        {
            return pressureIndex switch
            {
                >= 0.7000m => "danger",
                >= 0.5000m => "warning",
                _ => "info"
            };
        }

        private static int GetPopulationDistrictSeverityRank(string severity)
        {
            return severity switch
            {
                "danger" => 2,
                "warning" => 1,
                _ => 0
            };
        }

        private static string GetDistrictResponseSeverity(decimal priorityScore)
        {
            return priorityScore switch
            {
                >= 0.7000m => "danger",
                >= 0.5000m => "warning",
                _ => "info"
            };
        }

        private static int GetMobilitySeverityRank(string severity)
        {
            return severity switch
            {
                "danger" => 2,
                "warning" => 1,
                _ => 0
            };
        }

        private static int GetDistrictResponseSeverityRank(string severity)
        {
            return severity switch
            {
                "danger" => 2,
                "warning" => 1,
                _ => 0
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

        private static string GetTickFreshnessSeverity(long tickSkew)
        {
            return tickSkew switch
            {
                >= 5 => "danger",
                _ => "warning"
            };
        }

        private static int GetTickFreshnessSeverityRank(string severity)
        {
            return severity switch
            {
                "danger" => 2,
                "warning" => 1,
                _ => 0
            };
        }

        private static string GetPhaseProgressSeverity(
            bool orderingViolation,
            long tickSpread)
        {
            if (orderingViolation || tickSpread >= 2)
                return "danger";

            return "warning";
        }

        private static int GetPhaseProgressSeverityRank(string severity)
        {
            return severity switch
            {
                "danger" => 2,
                "warning" => 1,
                _ => 0
            };
        }

        private static string BuildTickFreshnessSummary(
            long environmentalTickId,
            long budgetTickId,
            long tickSkew)
        {
            if (environmentalTickId < budgetTickId)
                return
                    $"Environmental conditions are trailing budget state by {tickSkew} ticks and may be rendering a stale world frame.";

            if (budgetTickId < environmentalTickId)
                return
                    $"Budget state is trailing environmental conditions by {tickSkew} ticks and may be authorizing against stale city pressure.";

            return "Budget and environmental snapshots are aligned on the same world tick.";
        }

        private static string BuildPhaseProgressSummary(
            bool orderingViolation,
            CityEnvironmentalConditionsView conditions,
            CityStockpilesView stockpiles,
            CityOperationalBudgetPressureView budget,
            ServicePhaseState laggingState,
            ServicePhaseState leadingState,
            long tickSpread)
        {
            if (stockpiles.EffectiveTickId > conditions.EffectiveTickId)
                return
                    $"Resources reached {stockpiles.EffectivePhase} at tick {stockpiles.EffectiveTickId} ahead of simulation systems, so stockpile settlement is overtaking physical system degradation.";

            if (budget.EffectiveTickId > stockpiles.EffectiveTickId)
                return
                    $"Economy reached {budget.EffectivePhase} at tick {budget.EffectiveTickId} ahead of resources, so budget settlement is running in front of stockpile settlement.";

            if (orderingViolation)
                return
                    $"{leadingState.Service} has moved to {leadingState.Phase} at tick {leadingState.TickId} while {laggingState.Service} is still on {laggingState.Phase} at tick {laggingState.TickId}.";

            return
                $"{laggingState.Service} is still on {laggingState.Phase} at tick {laggingState.TickId} while {leadingState.Service} already reached {leadingState.Phase} at tick {leadingState.TickId}, leaving a phase spread of {tickSpread} tick(s).";
        }

        private static ServicePhaseState ResolveLaggingState(
            CityEnvironmentalConditionsView conditions,
            CityStockpilesView stockpiles,
            CityOperationalBudgetPressureView budget)
        {
            ServicePhaseState[] states =
            [
                new ServicePhaseState(
                    Service: "SimulationSystems",
                    TickId: conditions.EffectiveTickId,
                    Phase: conditions.EffectivePhase,
                    PhaseRank: GetPhaseRank(conditions.EffectivePhase)),
                new ServicePhaseState(
                    Service: "Resources",
                    TickId: stockpiles.EffectiveTickId,
                    Phase: stockpiles.EffectivePhase,
                    PhaseRank: GetPhaseRank(stockpiles.EffectivePhase)),
                new ServicePhaseState(
                    Service: "Economy",
                    TickId: budget.EffectiveTickId,
                    Phase: budget.EffectivePhase,
                    PhaseRank: GetPhaseRank(budget.EffectivePhase))
            ];

            long minTick = states.Min(state => state.TickId);

            return states
               .Where(state => state.TickId == minTick)
               .OrderBy(state => state.PhaseRank)
               .First();
        }

        private static ServicePhaseState ResolveLeadingState(
            CityEnvironmentalConditionsView conditions,
            CityStockpilesView stockpiles,
            CityOperationalBudgetPressureView budget)
        {
            ServicePhaseState[] states =
            [
                new ServicePhaseState(
                    Service: "SimulationSystems",
                    TickId: conditions.EffectiveTickId,
                    Phase: conditions.EffectivePhase,
                    PhaseRank: GetPhaseRank(conditions.EffectivePhase)),
                new ServicePhaseState(
                    Service: "Resources",
                    TickId: stockpiles.EffectiveTickId,
                    Phase: stockpiles.EffectivePhase,
                    PhaseRank: GetPhaseRank(stockpiles.EffectivePhase)),
                new ServicePhaseState(
                    Service: "Economy",
                    TickId: budget.EffectiveTickId,
                    Phase: budget.EffectivePhase,
                    PhaseRank: GetPhaseRank(budget.EffectivePhase))
            ];

            long maxTick = states.Max(state => state.TickId);

            return states
               .Where(state => state.TickId == maxTick)
               .OrderByDescending(state => state.PhaseRank)
               .First();
        }

        private static int GetPhaseRank(string phase)
        {
            return phase switch
            {
                "AdvanceTime" => 10,
                "SystemsDegradation" => 20,
                "IncidentGeneration" => 30,
                "DispatchExecution" => 40,
                "ResourceSettlement" => 50,
                "BudgetSettlement" => 60,
                "PopulationReaction" => 70,
                "Projection" => 80,
                "TickCompleted" => 90,
                _ => 0
            };
        }

        private static string BuildPopulationDistrictSummary(CityPopulationDistrictPressureItemDto district)
        {
            decimal severeIllnessBurden = district.ResidentCount <= 0
                ? 0m
                : (decimal)district.SevereIllnessCount / district.ResidentCount;
            decimal homelessnessBurden = district.ResidentCount <= 0
                ? 0m
                : (decimal)district.HomelessResidentCount / district.ResidentCount;
            decimal utilityFragility = ClampUnit(1m - ClampUnit(district.UtilityContinuityIndex));
            decimal dominantPressure = Max(
                severeIllnessBurden,
                homelessnessBurden,
                district.HousingFragilityIndex,
                district.UtilityIncidentPressureIndex,
                utilityFragility);

            if (severeIllnessBurden >= dominantPressure)
                return "One district is carrying a severe illness burden and local recovery conditions are starting to thin out.";

            if (homelessnessBurden >= dominantPressure || district.HousingFragilityIndex >= dominantPressure)
                return "One district is showing housing fragility and is starting to push more residents into unstable living conditions.";

            if (utilityFragility >= dominantPressure)
                return "One district is losing day-to-day utility continuity and basic living conditions are starting to fray.";

            if (district.UtilityIncidentPressureIndex >= dominantPressure)
                return "One district is stuck under sustained utility incident pressure and restoration is struggling to keep up.";

            return "One district is showing a compound mix of illness, housing stress, and unstable utilities.";
        }

        private static string BuildDistrictResponseSummary(
            CityPopulationDistrictPressureItemDto district,
            CityDistrictHeatingConditionView? heating,
            CityDistrictWaterDistributionConditionView? water,
            CityDistrictPowerDistributionConditionView? power,
            CityDistrictSanitationConditionView? sanitation,
            CityDistrictUtilityIncidentConditionView? incidents,
            decimal serviceDisruptionIndex,
            decimal maintenancePriorityIndex)
        {
            decimal heatingDisruption = heating is null ? 0m : 1m - heating.HeatingCoverageIndex;
            decimal waterDisruption = water is null ? 0m : 1m - water.WaterCoverageIndex;
            decimal powerDisruption = power is null ? 0m : 1m - power.PowerCoverageIndex;
            decimal sanitationDisruption = sanitation is null ? 0m : 1m - sanitation.SanitationCoverageIndex;
            decimal incidentPressure = incidents?.IncidentPressureIndex ?? district.UtilityIncidentPressureIndex;
            decimal dominantPressure = Max(
                district.PopulationPressureIndex,
                incidentPressure,
                serviceDisruptionIndex,
                maintenancePriorityIndex,
                heatingDisruption,
                waterDisruption,
                powerDisruption,
                sanitationDisruption);

            if (waterDisruption >= dominantPressure || sanitationDisruption >= dominantPressure)
                return "This district is losing basic water and sanitation stability and needs rapid service recovery before living conditions slip further.";

            if (powerDisruption >= dominantPressure || heatingDisruption >= dominantPressure)
                return "This district is carrying power and heating disruption that is starting to translate directly into social strain.";

            if (incidentPressure >= dominantPressure || maintenancePriorityIndex >= dominantPressure)
                return "This district is stuck in a hard restoration queue and should move up the operator response stack.";

            return "This district is showing the strongest combined social and utility strain in the city and is the best next response target.";
        }

        private static string BuildDistrictResponseFocus(
            CityPopulationDistrictPressureItemDto district,
            CityDistrictHeatingConditionView? heating,
            CityDistrictWaterDistributionConditionView? water,
            CityDistrictPowerDistributionConditionView? power,
            CityDistrictSanitationConditionView? sanitation,
            CityDistrictUtilityIncidentConditionView? incidents)
        {
            decimal heatingDisruption = heating is null ? 0m : 1m - heating.HeatingCoverageIndex;
            decimal waterDisruption = water is null ? 0m : 1m - water.WaterCoverageIndex;
            decimal powerDisruption = power is null ? 0m : 1m - power.PowerCoverageIndex;
            decimal sanitationDisruption = sanitation is null ? 0m : 1m - sanitation.SanitationCoverageIndex;
            decimal incidentPressure = incidents?.IncidentPressureIndex ?? district.UtilityIncidentPressureIndex;
            decimal dominantPressure = Max(
                waterDisruption,
                sanitationDisruption,
                powerDisruption,
                heatingDisruption,
                incidentPressure,
                district.HousingFragilityIndex,
                district.PopulationPressureIndex);

            if (waterDisruption >= dominantPressure || sanitationDisruption >= dominantPressure)
                return "Water and sanitation restoration";

            if (powerDisruption >= dominantPressure || heatingDisruption >= dominantPressure)
                return "Power and heating stabilization";

            if (incidentPressure >= dominantPressure)
                return "Incident coordination and dispatch";

            if (district.HousingFragilityIndex >= dominantPressure)
                return "Housing support and district stabilization";

            return "Integrated district response";
        }

        private static string BuildMobilitySummary(
            int activeCommuteCount,
            int activeHealthcareTripCount,
            decimal delayExposure,
            decimal dynamicRoadExposure,
            decimal averageRemainingTravelMinutes)
        {
            decimal dominantPressure = Max(
                activeCommuteCount,
                activeHealthcareTripCount * 2,
                delayExposure * 10m,
                dynamicRoadExposure * 10m,
                averageRemainingTravelMinutes / 10m);

            if ((activeHealthcareTripCount * 2) >= dominantPressure && delayExposure >= 0.3000m)
                return "Healthcare access trips are staying active under slower road conditions and medical movement is starting to stretch across the city.";

            if ((activeHealthcareTripCount * 2) >= dominantPressure)
                return "Healthcare access trips are starting to accumulate and medical movement demand is rising across the city.";

            if ((delayExposure * 10m) >= dominantPressure || (dynamicRoadExposure * 10m) >= dominantPressure)
                return "Active city trips are moving under degraded mobility conditions and commute flow is starting to stretch.";

            if (averageRemainingTravelMinutes >= 75m)
                return "Trips are staying active for longer than normal and the city is carrying heavier in-world movement load.";

            return "Commute movement is building up across the city and is becoming a new live operator signal.";
        }

        private static bool IsCommutePurpose(string purpose)
        {
            return string.Equals(
                       a: purpose,
                       b: "WorkCommute",
                       comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       a: purpose,
                       b: "EducationCommute",
                       comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHealthcarePurpose(string purpose)
        {
            return string.Equals(
                a: purpose,
                b: "HealthcareAccess",
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetTripSlowdownRatio(CityActiveTripView trip)
        {
            if (trip.PlannedTravelTimeMinutes <= 0m)
                return 1m;

            return decimal.Round(
                d: Math.Max(
                    1m,
                    trip.AdjustedTravelTimeMinutes / trip.PlannedTravelTimeMinutes),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal GetRemainingTravelMinutes(CityActiveTripView trip)
        {
            decimal minutes = (decimal)(trip.ExpectedArrivalAtSimTimeUtc - trip.LastAdvancedAtSimTimeUtc).TotalMinutes;

            return decimal.Round(
                d: Math.Max(0m, minutes),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
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

        private static long Max(params long[] values)
        {
            if (values.Length == 0)
                return 0L;

            long current = values[0];

            for (int index = 1; index < values.Length; index++)
                current = Math.Max(
                    val1: current,
                    val2: values[index]);

            return current;
        }

        private static long Min(params long[] values)
        {
            if (values.Length == 0)
                return 0L;

            long current = values[0];

            for (int index = 1; index < values.Length; index++)
                current = Math.Min(
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

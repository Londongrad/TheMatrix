using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
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
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal sealed class CityOperationsDashboardSnapshotLoader(
        IEconomyApiClient economyClient,
        IPopulationApiClient populationClient,
        IStockpilesApiClient stockpilesClient,
        ITripsApiClient tripsClient,
        IEnvironmentalConditionsApiClient environmentalConditionsClient,
        IOptions<CityOperationsDashboardOptions> dashboardOptions,
        ILogger<CityOperationsDashboardSnapshotLoader> logger) : ICityOperationsDashboardSnapshotLoader
    {
        private readonly CityOperationsDashboardOptions _dashboardOptions = dashboardOptions.Value;
        private readonly IEconomyApiClient _economyClient = economyClient;

        private readonly IEnvironmentalConditionsApiClient _environmentalConditionsClient =
            environmentalConditionsClient;

        private readonly ILogger<CityOperationsDashboardSnapshotLoader> _logger = logger;
        private readonly IPopulationApiClient _populationClient = populationClient;
        private readonly IStockpilesApiClient _stockpilesClient = stockpilesClient;
        private readonly ITripsApiClient _tripsClient = tripsClient;

        public async Task<IReadOnlyList<CityOperationalSnapshot>> LoadReadyClassicCitySnapshotsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken)
        {
            CityListItemView[] readyClassicCities = allCities
               .Where(IsReady)
               .ToArray();

            if (readyClassicCities.Length == 0)
                return [];

            var snapshots = new CityOperationalSnapshot[readyClassicCities.Length];
            using var gate = new SemaphoreSlim(
                initialCount: _dashboardOptions.MaxConcurrentCitySnapshotLoads,
                maxCount: _dashboardOptions.MaxConcurrentCitySnapshotLoads);

            Task[] snapshotTasks = readyClassicCities
               .Select((
                    city,
                    index) => LoadReadyClassicCitySnapshotWithGateAsync(
                    city: city,
                    index: index,
                    snapshots: snapshots,
                    gate: gate,
                    cancellationToken: cancellationToken))
               .ToArray();

            await Task.WhenAll(snapshotTasks);

            return snapshots;
        }

        private async Task LoadReadyClassicCitySnapshotWithGateAsync(
            CityListItemView city,
            int index,
            CityOperationalSnapshot[] snapshots,
            SemaphoreSlim gate,
            CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);

            try
            {
                snapshots[index] = await LoadReadyClassicCitySnapshotAsync(
                    city: city,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<CityOperationalSnapshot> LoadReadyClassicCitySnapshotAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            Task<CityEnvironmentalConditionsView?> environmentalTask = TryLoadEnvironmentalConditionsAsync(
                city: city,
                cancellationToken: cancellationToken);
            Task<CityPopulationDistrictPressureDto?> populationDistrictPressureTask =
                TryLoadPopulationDistrictPressureAsync(
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
            Task<CityDistrictSanitationConditionsView?> districtSanitationTask =
                TryLoadDistrictSanitationConditionsAsync(
                    city: city,
                    cancellationToken: cancellationToken);
            Task<CityDistrictUtilityIncidentConditionsView?> districtUtilityIncidentsTask =
                TryLoadDistrictUtilityIncidentConditionsAsync(
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

        private async Task<CityEnvironmentalConditionsView?> TryLoadEnvironmentalConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityEnvironmentalConditionsView>(
                city: city,
                serviceName: "SimulationSystems",
                load: token => _environmentalConditionsClient.GetCityEnvironmentalConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach simulation systems metrics to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped simulation systems metrics for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityPopulationDistrictPressureDto?> TryLoadPopulationDistrictPressureAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityPopulationDistrictPressureDto>(
                city: city,
                serviceName: "Population",
                load: async token => await _populationClient.GetCityDistrictPressureAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach population district pressure to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped population district pressure for city operations dashboard because Population returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictHeatingConditionsView?> TryLoadDistrictHeatingConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityDistrictHeatingConditionsView>(
                city: city,
                serviceName: "SimulationSystems",
                load: token => _environmentalConditionsClient.GetCityDistrictHeatingConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach district heating conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district heating conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictWaterDistributionConditionsView?> TryLoadDistrictWaterConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityDistrictWaterDistributionConditionsView>(
                city: city,
                serviceName: "SimulationSystems",
                load: token => _environmentalConditionsClient.GetCityDistrictWaterDistributionConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach district water conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district water conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictPowerDistributionConditionsView?> TryLoadDistrictPowerConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityDistrictPowerDistributionConditionsView>(
                city: city,
                serviceName: "SimulationSystems",
                load: token => _environmentalConditionsClient.GetCityDistrictPowerDistributionConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach district power conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district power conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictSanitationConditionsView?> TryLoadDistrictSanitationConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityDistrictSanitationConditionsView>(
                city: city,
                serviceName: "SimulationSystems",
                load: token => _environmentalConditionsClient.GetCityDistrictSanitationConditionsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach district sanitation conditions to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped district sanitation conditions for city operations dashboard because SimulationSystems returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityDistrictUtilityIncidentConditionsView?> TryLoadDistrictUtilityIncidentConditionsAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityDistrictUtilityIncidentConditionsView>(
                city: city,
                serviceName: "SimulationSystems",
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
            return await TryLoadDashboardReadAsync<IReadOnlyList<CityActiveTripView>>(
                city: city,
                serviceName: "SimulationCore",
                load: async token => await _tripsClient.GetActiveTripsAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach active trips to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped active trips for city operations dashboard because SimulationCore returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityOperationalBudgetPressureView?> TryLoadBudgetPressureAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityOperationalBudgetPressureView>(
                city: city,
                serviceName: "Economy",
                load: token => _economyClient.GetCityOperationalBudgetPressureAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach economy operational pressure to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped economy operational pressure for city operations dashboard because Economy returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<CityStockpilesView?> TryLoadStockpilesAsync(
            CityListItemView city,
            CancellationToken cancellationToken)
        {
            return await TryLoadDashboardReadAsync<CityStockpilesView>(
                city: city,
                serviceName: "Resources",
                load: token => _stockpilesClient.GetCityStockpilesAsync(
                    cityId: city.CityId,
                    cancellationToken: token),
                failureMessage:
                "Failed to attach resource stockpiles to city operations dashboard for cityId={CityId}.",
                skippedMessage:
                "Skipped resource stockpiles for city operations dashboard because Resources returned status {StatusCode} for cityId={CityId}.",
                cancellationToken: cancellationToken);
        }

        private async Task<T?> TryLoadDashboardReadAsync<T>(
            CityListItemView city,
            string serviceName,
            Func<CancellationToken, Task<T?>> load,
            string failureMessage,
            string skippedMessage,
            CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_dashboardOptions.PanelReadTimeoutSeconds));
                return await load(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Timed out attaching {ServiceName} dashboard panel data for cityId={CityId} after {TimeoutSeconds}s.",
                    serviceName,
                    city.CityId,
                    _dashboardOptions.PanelReadTimeoutSeconds);

                return null;
            }
            catch (HttpRequestException exception)
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

        private static bool IsReady(CityListItemView city)
        {
            return city.ArchivedAtUtc is null &&
                   city.Status.Equals(
                       value: "Active",
                       comparisonType: StringComparison.OrdinalIgnoreCase);
        }

    }
}

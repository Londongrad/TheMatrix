using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    internal sealed class CityOperationsDashboardRecentEventsBuilder : ICityOperationsDashboardRecentEventsBuilder
    {
        public DashboardRecentEventView[] Build(IReadOnlyList<CityListItemView> cities)
        {
            ArgumentNullException.ThrowIfNull(cities);

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
    }
}

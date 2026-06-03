using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Dashboard
{
    public sealed class CityOperationsDashboardRecentEventsBuilderTests
    {
        [Fact]
        public void Build_WhenNoCitiesExist_ReturnsEmptyEvents()
        {
            var builder = new CityOperationsDashboardRecentEventsBuilder();

            DashboardRecentEventView[] events = builder.Build([]);

            Assert.Empty(events);
        }

        [Fact]
        public void Build_WhenCityBecameReady_ReturnsReadyEvent()
        {
            DateTimeOffset completedAtUtc = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 10,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            CityListItemView city = CreateCity(
                name: "Ready City",
                populationBootstrapCompletedAtUtc: completedAtUtc);
            var builder = new CityOperationsDashboardRecentEventsBuilder();

            DashboardRecentEventView[] events = builder.Build([city]);

            DashboardRecentEventView readyEvent = Assert.Single(
                collection: events,
                predicate: @event => @event.Kind == "bootstrap-ready");
            Assert.Equal(
                expected: "success",
                actual: readyEvent.Severity);
            Assert.Equal(
                expected: "Provisioning completed",
                actual: readyEvent.Title);
            Assert.Equal(
                expected: city.CityId,
                actual: readyEvent.CityId);
            Assert.Equal(
                expected: city.Name,
                actual: readyEvent.CityName);
            Assert.Equal(
                expected: completedAtUtc,
                actual: readyEvent.OccurredAtUtc);
        }

        [Fact]
        public void Build_WhenBootstrapFailedWithoutCode_UsesFallbackFailureDetail()
        {
            DateTimeOffset failedAtUtc = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 11,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            CityListItemView city = CreateCity(
                name: "Failed City",
                status: "ProvisioningFailed",
                populationBootstrapFailedAtUtc: failedAtUtc,
                populationBootstrapFailureCode: null);
            var builder = new CityOperationsDashboardRecentEventsBuilder();

            DashboardRecentEventView[] events = builder.Build([city]);

            DashboardRecentEventView failureEvent = Assert.Single(
                collection: events,
                predicate: @event => @event.Kind == "bootstrap-failed");
            Assert.Equal(
                expected: "danger",
                actual: failureEvent.Severity);
            Assert.Equal(
                expected: "Provisioning failed",
                actual: failureEvent.Title);
            Assert.Equal(
                expected: "Population bootstrap failed before the city became ready.",
                actual: failureEvent.Detail);
            Assert.Equal(
                expected: city.CityId,
                actual: failureEvent.CityId);
            Assert.Equal(
                expected: failedAtUtc,
                actual: failureEvent.OccurredAtUtc);
        }

        [Fact]
        public void Build_WhenMultipleEventsExist_SortsByOccurrenceThenCityName()
        {
            DateTimeOffset sameInstant = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            CityListItemView alpha = CreateCity(
                name: "Alpha",
                createdAtUtc: sameInstant);
            CityListItemView bravo = CreateCity(
                name: "Bravo",
                createdAtUtc: sameInstant);
            CityListItemView newest = CreateCity(
                name: "Newest",
                createdAtUtc: sameInstant.AddMinutes(1));
            var builder = new CityOperationsDashboardRecentEventsBuilder();

            DashboardRecentEventView[] events = builder.Build(
            [
                bravo,
                newest,
                alpha
            ]);

            Assert.Equal(
                expectedSpan:
                [
                    "Newest",
                    "Alpha",
                    "Bravo"
                ],
                actualArray: events.Select(@event => @event.CityName)
                   .ToArray());
        }

        [Fact]
        public void Build_WhenMoreThanTenEventsExist_ReturnsTenMostRecentEvents()
        {
            DateTimeOffset start = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 0,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            CityListItemView[] cities = Enumerable
               .Range(
                    start: 0,
                    count: 12)
               .Select(index => CreateCity(
                    name: $"City {index:00}",
                    createdAtUtc: start.AddMinutes(index)))
               .ToArray();
            var builder = new CityOperationsDashboardRecentEventsBuilder();

            DashboardRecentEventView[] events = builder.Build(cities);

            Assert.Equal(
                expected: 10,
                actual: events.Length);
            Assert.Equal(
                expected: "City 11",
                actual: events[0].CityName);
            Assert.Equal(
                expected: "City 02",
                actual: events[^1].CityName);
            Assert.DoesNotContain(
                collection: events,
                filter: @event => @event.CityName == "City 00");
            Assert.DoesNotContain(
                collection: events,
                filter: @event => @event.CityName == "City 01");
        }

        private static CityListItemView CreateCity(
            string name = "Neo City",
            string status = "Active",
            DateTimeOffset? createdAtUtc = null,
            DateTimeOffset? populationBootstrapCompletedAtUtc = null,
            DateTimeOffset? populationBootstrapFailedAtUtc = null,
            string? populationBootstrapFailureCode = null,
            DateTimeOffset? archivedAtUtc = null)
        {
            DateTimeOffset created = createdAtUtc ??
                                     new DateTimeOffset(
                                         year: 2048,
                                         month: 1,
                                         day: 1,
                                         hour: 0,
                                         minute: 0,
                                         second: 0,
                                         offset: TimeSpan.Zero);

            return new CityListItemView(
                CityId: Guid.NewGuid(),
                SimulationId: Guid.NewGuid(),
                Name: name,
                Status: status,
                CreatedAtUtc: created,
                PopulationBootstrapCompletedAtUtc: populationBootstrapCompletedAtUtc,
                PopulationBootstrapFailedAtUtc: populationBootstrapFailedAtUtc,
                PopulationBootstrapFailureCode: populationBootstrapFailureCode,
                ArchivedAtUtc: archivedAtUtc);
        }
    }
}

using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Dashboard;

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
        DateTimeOffset completedAtUtc = new(2048, 6, 1, 10, 0, 0, TimeSpan.Zero);
        CityListItemView city = CreateCity(
            name: "Ready City",
            populationBootstrapCompletedAtUtc: completedAtUtc);
        var builder = new CityOperationsDashboardRecentEventsBuilder();

        DashboardRecentEventView[] events = builder.Build([city]);

        DashboardRecentEventView readyEvent = Assert.Single(
            events,
            @event => @event.Kind == "bootstrap-ready");
        Assert.Equal("success", readyEvent.Severity);
        Assert.Equal("Provisioning completed", readyEvent.Title);
        Assert.Equal(city.CityId, readyEvent.CityId);
        Assert.Equal(city.Name, readyEvent.CityName);
        Assert.Equal(completedAtUtc, readyEvent.OccurredAtUtc);
    }

    [Fact]
    public void Build_WhenBootstrapFailedWithoutCode_UsesFallbackFailureDetail()
    {
        DateTimeOffset failedAtUtc = new(2048, 6, 1, 11, 0, 0, TimeSpan.Zero);
        CityListItemView city = CreateCity(
            name: "Failed City",
            status: "ProvisioningFailed",
            populationBootstrapFailedAtUtc: failedAtUtc,
            populationBootstrapFailureCode: null);
        var builder = new CityOperationsDashboardRecentEventsBuilder();

        DashboardRecentEventView[] events = builder.Build([city]);

        DashboardRecentEventView failureEvent = Assert.Single(
            events,
            @event => @event.Kind == "bootstrap-failed");
        Assert.Equal("danger", failureEvent.Severity);
        Assert.Equal("Provisioning failed", failureEvent.Title);
        Assert.Equal("Population bootstrap failed before the city became ready.", failureEvent.Detail);
        Assert.Equal(city.CityId, failureEvent.CityId);
        Assert.Equal(failedAtUtc, failureEvent.OccurredAtUtc);
    }

    [Fact]
    public void Build_WhenMultipleEventsExist_SortsByOccurrenceThenCityName()
    {
        DateTimeOffset sameInstant = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
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

        DashboardRecentEventView[] events = builder.Build([bravo, newest, alpha]);

        Assert.Equal(
            ["Newest", "Alpha", "Bravo"],
            events.Select(@event => @event.CityName).ToArray());
    }

    [Fact]
    public void Build_WhenMoreThanTenEventsExist_ReturnsTenMostRecentEvents()
    {
        DateTimeOffset start = new(2048, 6, 1, 0, 0, 0, TimeSpan.Zero);
        CityListItemView[] cities = Enumerable
           .Range(0, 12)
           .Select(index => CreateCity(
                name: $"City {index:00}",
                createdAtUtc: start.AddMinutes(index)))
           .ToArray();
        var builder = new CityOperationsDashboardRecentEventsBuilder();

        DashboardRecentEventView[] events = builder.Build(cities);

        Assert.Equal(10, events.Length);
        Assert.Equal("City 11", events[0].CityName);
        Assert.Equal("City 02", events[^1].CityName);
        Assert.DoesNotContain(events, @event => @event.CityName == "City 00");
        Assert.DoesNotContain(events, @event => @event.CityName == "City 01");
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
        DateTimeOffset created = createdAtUtc ?? new DateTimeOffset(
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
            SimulationKind: "ClassicCity",
            Status: status,
            CreatedAtUtc: created,
            PopulationBootstrapCompletedAtUtc: populationBootstrapCompletedAtUtc,
            PopulationBootstrapFailedAtUtc: populationBootstrapFailedAtUtc,
            PopulationBootstrapFailureCode: populationBootstrapFailureCode,
            ArchivedAtUtc: archivedAtUtc);
    }
}

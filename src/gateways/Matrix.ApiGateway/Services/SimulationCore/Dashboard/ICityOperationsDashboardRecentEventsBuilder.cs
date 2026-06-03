using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal interface ICityOperationsDashboardRecentEventsBuilder
    {
        DashboardRecentEventView[] Build(IReadOnlyList<CityListItemView> cities);
    }
}

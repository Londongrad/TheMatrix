namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;

public static class ClassicCityApiRoutes
{
    public const string CitiesRoute = "api/cities";
    public const string CitiesPath = "/" + CitiesRoute;
    public const string TripsRoute = CitiesRoute + "/{cityId:guid}/trips";
    public const string RoutingRoute = CitiesRoute + "/{cityId:guid}/routes";
}

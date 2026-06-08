namespace Matrix.Population.Contracts.Scenarios.ClassicCity;

public static class ClassicCityPopulationApiRoutes
{
    public const string PopulationRoute = "api/scenarios/classic-city/population";
    public const string PopulationPath = "/" + PopulationRoute;
    public const string InitializePath = PopulationPath + "/init";
    public const string CitiesPath = PopulationPath + "/cities";
}

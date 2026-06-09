namespace Matrix.Population.Contracts.Scenarios.ClassicCity;

public static class ClassicCityPopulationApiRoutes
{
    public const string PopulationRoute = "api/scenarios/classic-city/population";
    public const string PopulationPath = "/" + PopulationRoute;
    public const string InitializePath = PopulationPath + "/init";
    public const string CitiesRoute = PopulationRoute + "/cities";
    public const string CitiesPath = "/" + CitiesRoute;
    public const string CityRoute = CitiesRoute + "/{cityId:guid}";
    public const string ResidentsRoute = CityRoute + "/residents";
    public const string EmploymentRoute = CityRoute + "/employment";
    public const string EducationRoute = CityRoute + "/education";
    public const string CivilRegistryRoute = CityRoute + "/civil-registry";
}

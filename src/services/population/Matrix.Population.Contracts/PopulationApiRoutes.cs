namespace Matrix.Population.Contracts;

public static class PopulationApiRoutes
{
    public const string PopulationRoute = "api/population";
    public const string PeopleRoute = PopulationRoute + "/citizens";
    public const string PeoplePath = "/" + PeopleRoute;
    public const string PersonRoute = "api/person/{personId:guid}";
    public const string PersonPath = "/api/person";
}

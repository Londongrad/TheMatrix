namespace Matrix.Population.Contracts;

public static class PopulationApiRoutes
{
    public const string PopulationRoute = "api/population";
    public const string PeopleRoute = PopulationRoute + "/people";
    public const string PeoplePath = "/" + PeopleRoute;
    public const string PersonRoute = PeopleRoute + "/{personId:guid}";
    public const string PersonPath = PeoplePath;
}

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources
{
    public enum DeleteCityResourcesStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2
    }

    public sealed record DeleteCityResourcesResult(DeleteCityResourcesStatus Status);
}

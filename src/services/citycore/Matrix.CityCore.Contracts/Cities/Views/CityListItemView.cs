namespace Matrix.CityCore.Contracts.Cities.Views
{
    public sealed record CityListItemView(
        Guid CityId,
        string Name,
        string Status);
}

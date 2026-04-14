namespace Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views
{
    public sealed record PendingResupplyView(
        string Focus,
        string Intensity,
        Guid? FocusDistrictId,
        long ReadyAtTickId);
}

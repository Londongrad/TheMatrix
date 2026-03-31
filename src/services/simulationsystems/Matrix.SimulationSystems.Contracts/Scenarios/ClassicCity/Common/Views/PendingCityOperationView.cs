namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views
{
    public sealed record PendingCityOperationView(
        string Focus,
        string Intensity,
        long ReadyAtTickId);
}

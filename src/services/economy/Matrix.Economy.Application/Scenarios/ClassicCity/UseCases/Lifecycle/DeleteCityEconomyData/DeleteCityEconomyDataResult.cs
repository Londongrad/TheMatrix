namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData
{
    public enum DeleteCityEconomyDataStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2
    }

    public sealed record DeleteCityEconomyDataResult(DeleteCityEconomyDataStatus Status);
}

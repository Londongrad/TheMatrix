namespace Matrix.CityCore.Application.UseCases.GetCurrentSimulationTime
{
    public sealed record SimulationTimeDto(
        DateTime CurrentTime,
        int SimMinutesPerTick,
        bool IsPaused);
}

namespace Matrix.CityCore.Infrastructure.Simulation
{
    public sealed class SimulationLoopSettings
    {
        /// <summary>Интервал реального времени между тиками, в миллисекундах.</summary>
        public int RealTimeTickMilliseconds { get; init; } = 1000;

        /// <summary>Сколько игровых минут проходит за один тик.</summary>
        public int SimMinutesPerTick { get; init; } = 5;
    }
}

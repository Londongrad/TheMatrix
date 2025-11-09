namespace Matrix.CityCore.Domain.ValueObjects
{
    public sealed class SimulationTime(DateTime current)
    {
        public DateTime Current { get; } = current;

        public SimulationTime AddMinutes(int minutes)
            => new(Current.AddMinutes(minutes));

        public override string ToString() => Current.ToString("O");
    }
}

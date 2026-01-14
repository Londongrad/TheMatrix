namespace Matrix.CityCore.Domain.ValueObjects
{
    public sealed class SimulationTime(DateTime current)
    {
        public DateTime Current { get; } = current;

        public SimulationTime AddMinutes(int minutes)
        {
            return new SimulationTime(Current.AddMinutes(minutes));
        }

        public override string ToString()
        {
            return Current.ToString("O");
        }
    }
}

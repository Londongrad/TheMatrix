using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Time
{
    /// <summary>
    ///     Simulation time (virtual city time). This is NOT system time.
    /// </summary>
    public readonly record struct SimTime(DateTimeOffset ValueUtc) : IComparable<SimTime>
    {
        public int CompareTo(SimTime other)
        {
            return ValueUtc.CompareTo(other.ValueUtc);
        }

        public static SimTime FromUtc(DateTimeOffset utc)
        {
            GuardHelper.Ensure(
                condition: utc.Offset == TimeSpan.Zero,
                value: utc,
                errorFactory: DomainErrorsFactory.SimTimeMustBeUtc);

            return new SimTime(utc);
        }

        public SimTime Add(TimeSpan delta)
        {
            return FromUtc(ValueUtc + delta);
        }

        public override string ToString()
        {
            return ValueUtc.ToString("O");
        }
    }
}

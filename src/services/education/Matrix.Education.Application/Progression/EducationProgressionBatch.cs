using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Application.Progression
{
    public sealed record EducationProgressionBatch
    {
        private EducationProgressionBatch(
            SimulationHostId simulationHostId,
            long tickId,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            SimulationHostId = simulationHostId;
            TickId = tickId;
            FromSimTimeUtc = fromSimTimeUtc;
            ToSimTimeUtc = toSimTimeUtc;
        }

        public SimulationHostId SimulationHostId { get; }
        public long TickId { get; }
        public DateTimeOffset FromSimTimeUtc { get; }
        public DateTimeOffset ToSimTimeUtc { get; }

        public static EducationProgressionBatch Create(
            SimulationHostId simulationHostId,
            long tickId,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            if (tickId < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(tickId),
                    message: "Progression tick identifiers cannot be negative.");

            EnsureUtc(fromSimTimeUtc, nameof(fromSimTimeUtc));
            EnsureUtc(toSimTimeUtc, nameof(toSimTimeUtc));

            if (toSimTimeUtc < fromSimTimeUtc)
                throw new ArgumentException(
                    message: "Education progression time cannot move backwards.",
                    paramName: nameof(toSimTimeUtc));

            return new EducationProgressionBatch(
                simulationHostId: simulationHostId,
                tickId: tickId,
                fromSimTimeUtc: fromSimTimeUtc,
                toSimTimeUtc: toSimTimeUtc);
        }

        private static void EnsureUtc(DateTimeOffset value, string parameterName)
        {
            if (value.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Progression timestamps must be expressed in UTC.",
                    paramName: parameterName);
        }
    }
}

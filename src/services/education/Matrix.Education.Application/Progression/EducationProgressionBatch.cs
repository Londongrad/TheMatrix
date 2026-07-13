using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Progression
{
    public sealed record EducationProgressionBatch
    {
        private EducationProgressionBatch(
            SimulationRuntimeKey runtimeKey,
            SimulationHostId simulationHostId,
            long tickId,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            RuntimeKey = runtimeKey;
            SimulationHostId = simulationHostId;
            TickId = tickId;
            FromSimTimeUtc = fromSimTimeUtc;
            ToSimTimeUtc = toSimTimeUtc;
        }

        public SimulationRuntimeKey RuntimeKey { get; }
        public SimulationHostId SimulationHostId { get; }
        public long TickId { get; }
        public DateTimeOffset FromSimTimeUtc { get; }
        public DateTimeOffset ToSimTimeUtc { get; }

        public static EducationProgressionBatch Create(
            SimulationRuntimeKey runtimeKey,
            SimulationHostId simulationHostId,
            long tickId,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            if (runtimeKey.IsEmpty)
                throw new ArgumentException(
                    message: "Education progression requires a simulation runtime key.",
                    paramName: nameof(runtimeKey));

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
                runtimeKey: runtimeKey,
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

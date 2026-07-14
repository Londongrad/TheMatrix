using Matrix.Education.Application.Progression.AdvanceEducationProgression;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;

namespace Matrix.Education.Integration.Scenarios.ClassicCity.Consumers
{
    internal static class ClassicCityEducationProgressionCommandMapper
    {
        internal static AdvanceEducationProgressionCommand Map(
            SimulationTickPhaseReachedV1 message)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey))
                throw new ArgumentException(
                    "Only Classic City simulation ticks can advance Classic City education.",
                    nameof(message));
            if (!string.Equals(
                    message.PhaseKey,
                    ClassicCityTickPhaseKeys.Projection,
                    StringComparison.Ordinal))
                throw new ArgumentException(
                    "Classic City education advances during the projection phase.",
                    nameof(message));

            return new AdvanceEducationProgressionCommand(
                SimulationHostId: message.HostId,
                ScenarioKey: message.ScenarioKey,
                HostTypeKey: message.HostTypeKey,
                TickId: message.TickId,
                FromSimTimeUtc: message.FromSimTimeUtc,
                ToSimTimeUtc: message.ToSimTimeUtc);
        }
    }
}

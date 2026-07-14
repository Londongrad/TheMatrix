using Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using Xunit;

namespace Matrix.Education.Integration.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEducationProgressionCommandMapperTests
    {
        [Fact]
        public void Map_ProjectionTick_PreservesRuntimeAndSimulationTime()
        {
            SimulationTickPhaseReachedV1 message = CreateMessage();

            var command = ClassicCityEducationProgressionCommandMapper.Map(message);

            Assert.Equal(message.HostId, command.SimulationHostId);
            Assert.Equal(message.ScenarioKey, command.ScenarioKey);
            Assert.Equal(message.HostTypeKey, command.HostTypeKey);
            Assert.Equal(message.TickId, command.TickId);
            Assert.Equal(message.FromSimTimeUtc, command.FromSimTimeUtc);
            Assert.Equal(message.ToSimTimeUtc, command.ToSimTimeUtc);
        }

        [Theory]
        [InlineData("metro-2033", "station-network", "projection")]
        [InlineData("classic-city", "city", "population-reaction")]
        public void Map_UnsupportedRuntimeOrPhase_Throws(
            string scenarioKey,
            string hostTypeKey,
            string phaseKey)
        {
            SimulationTickPhaseReachedV1 message = CreateMessage(
                scenarioKey,
                hostTypeKey,
                phaseKey);

            Assert.Throws<ArgumentException>(() =>
                ClassicCityEducationProgressionCommandMapper.Map(message));
        }

        private static SimulationTickPhaseReachedV1 CreateMessage(
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey,
            string phaseKey = ClassicCityTickPhaseKeys.Projection)
        {
            return new SimulationTickPhaseReachedV1(
                SimulationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                HostId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ScenarioKey: scenarioKey,
                HostTypeKey: hostTypeKey,
                PhaseKey: phaseKey,
                FromSimTimeUtc: new DateTimeOffset(2048, 6, 1, 0, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 6, 1, 0, 1, 0, TimeSpan.Zero),
                TickId: 42,
                SpeedMultiplier: 1m,
                ModelVersion: 1,
                CausationId: "tick:42:projection",
                CorrelationId: "tick:42",
                OccurredOnUtc: new DateTime(2048, 6, 1, 0, 0, 1, DateTimeKind.Utc));
        }
    }
}

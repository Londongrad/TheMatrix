using Matrix.Education.Integration.Consumers;
using Matrix.SimulationCore.Contracts.Events;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers;

public sealed class SimulationCreatedConsumerTests
{
    [Fact]
    public void Map_UsesHostIdentityAndExplicitRuntime()
    {
        var message = new SimulationCreatedV1(Guid.NewGuid(), Guid.NewGuid(), "test-scenario", "network",
            "seed", Guid.NewGuid(), "v1", null, "ready", DateTimeOffset.UtcNow);
        var command = SimulationCreatedConsumer.Map(message);
        Assert.Equal(message.HostId, command.SimulationHostId);
        Assert.Equal(message.ScenarioKey, command.ScenarioKey);
        Assert.Equal(message.HostTypeKey, command.HostTypeKey);
        Assert.NotEqual(message.SimulationId, command.SimulationHostId);
    }
}

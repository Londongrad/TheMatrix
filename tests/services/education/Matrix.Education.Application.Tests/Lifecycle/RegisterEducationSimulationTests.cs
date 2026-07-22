using Matrix.Education.Application.Lifecycle.RegisterEducationSimulation;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Lifecycle;

public sealed class RegisterEducationSimulationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_RegistersOnlyNonDeletedHosts(bool deleted)
    {
        var repository = new EducationSimulationRuntimeRepositoryStub();
        var handler = new RegisterEducationSimulationCommandHandler(repository,
            new EducationSimulationDeletionRepositoryStub(deleted ? DateTimeOffset.UtcNow : null),
            new EducationUnitOfWorkStub());
        var command = new RegisterEducationSimulationCommand(Guid.NewGuid(), "test-scenario", "network");

        Assert.Equal(!deleted, await handler.Handle(command, CancellationToken.None));
        if (deleted)
            Assert.Empty(repository.Runtimes);
        else
            Assert.Equal(new SimulationRuntimeKey(new SimulationScenarioKey("test-scenario"), new SimulationHostTypeKey("network")),
                repository.Runtimes[new SimulationHostId(command.SimulationHostId)]);
    }

    [Fact]
    public async Task Handle_DuplicateIsIdempotentAndDifferentRuntimeIsRejected()
    {
        var repository = new EducationSimulationRuntimeRepositoryStub();
        var handler = new RegisterEducationSimulationCommandHandler(repository,
            new EducationSimulationDeletionRepositoryStub(), new EducationUnitOfWorkStub());
        var command = new RegisterEducationSimulationCommand(Guid.NewGuid(), "classic-city", "city");
        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);
        Assert.Single(repository.Runtimes);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command with { ScenarioKey = "other" }, CancellationToken.None));
    }
}

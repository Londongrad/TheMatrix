using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Integration.Education.ApplyEducationAttendance;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Integration.Education;

public sealed class ApplyEducationAttendanceTests
{
    [Theory]
    [InlineData("current", 1)]
    [InlineData("missing", 0)]
    [InlineData("lifecycle", 0)]
    [InlineData("dead", 0)]
    public async Task Handle_AppliesOnlyToExistingLiveResidentWithMatchingLifecycle(string state, int expected)
    {
        var resident = CreatePerson(Guid.NewGuid());
        var repository = new FakePersonReadRepository();
        if (state != "missing") repository.PersonsById.Add(resident.Id, resident);
        if (state == "dead") resident.Die(new DateOnly(2048, 5, 3));
        var writer = new Writer();
        var unitOfWork = new FakeUnitOfWork();
        var command = new ApplyEducationAttendanceCommand(Guid.NewGuid(), 5, UtcNow,
            [new(resident.Id.Value, state == "lifecycle" ? 1 : resident.LifecycleRevision, 1, 0.7m, 0.8m)]);
        int count = await new ApplyEducationAttendanceCommandHandler(repository, writer, unitOfWork).Handle(command, default);
        Assert.Equal(expected, count);
        Assert.Equal(expected, writer.Inputs.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        if (expected == 1) Assert.Equal(command.SimulationHostId, writer.HostId);
    }

    [Fact]
    public async Task Handle_RejectsMalformedBatchBeforeTransaction()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApplyEducationAttendanceCommandHandler(new FakePersonReadRepository(), new Writer(), unitOfWork);
        EducationAttendanceInput input = new(Guid.NewGuid(), 0, 1, 0.7m, 0.8m);
        var command = new ApplyEducationAttendanceCommand(Guid.NewGuid(), 5, UtcNow, [input]);
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command with { Residents = [input, input] }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command with
            { Residents = [input with { AttendanceIndex = 2m }] }, default));
        Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
    }

    private sealed class Writer : IEducationAttendanceProjectionWriter
    {
        public IReadOnlyCollection<EducationAttendanceInput> Inputs { get; private set; } = [];
        public Guid HostId { get; private set; }
        public Task<int> ApplyAsync(Guid simulationHostId, long sourceTickId, DateTimeOffset observedAtSimTimeUtc,
            IReadOnlyCollection<EducationAttendanceInput> residents, CancellationToken cancellationToken)
        {
            HostId = simulationHostId;
            Inputs = residents;
            return Task.FromResult(residents.Count);
        }
    }
}

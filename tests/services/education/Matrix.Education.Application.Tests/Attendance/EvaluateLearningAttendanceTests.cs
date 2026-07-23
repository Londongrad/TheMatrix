using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Scenarios.ClassicCity.Attendance;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Attendance;

public sealed class EvaluateLearningAttendanceTests
{
    private static readonly DateTimeOffset Now = new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_RecordsAndPublishesOnceInSerializableTransaction()
    {
        var profile = CreateProfile();
        var outbox = new AttendanceOutbox();
        var unitOfWork = new EducationUnitOfWorkStub();
        var repository = new StudentProfileRepositoryStub([profile]);
        var handler = CreateHandler(repository, outbox, unitOfWork);
        var command = CreateCommand(profile);
        Assert.Equal(1, await handler.Handle(command, default));
        Assert.Equal(0, await handler.Handle(command, default));
        Assert.Equal(0, await handler.Handle(command with { SourceTickId = 4 }, default));
        var batch = Assert.Single(outbox.Batches);
        Assert.Equal(profile.SimulationHostId.Value, batch.SimulationHostId);
        Assert.Equal(0.61m, Assert.Single(batch.Residents).AttendanceIndex);
        Assert.Equal(0.5m, profile.CommuteAccessibilityIndex);
        Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        Assert.Equal(3, repository.GetCallCount);
    }

    [Theory]
    [InlineData("host")]
    [InlineData("lifecycle")]
    [InlineData("participation")]
    [InlineData("inactive")]
    public async Task Handle_DoesNotApplyToChangedOrUnrelatedStudent(string mismatch)
    {
        var profile = CreateProfile();
        var command = CreateCommand(profile);
        if (mismatch == "host") command = command with { SimulationHostId = Guid.NewGuid() };
        if (mismatch == "lifecycle") command = command with { Residents = [command.Residents[0] with { LifecycleRevision = 1 }] };
        if (mismatch == "participation") profile.RecordParticipationChange();
        if (mismatch == "inactive") profile.TryDeactivate(2, Now);
        var outbox = new AttendanceOutbox();
        Assert.Equal(0, await CreateHandler(new([profile]), outbox, new()).Handle(command, default));
        Assert.Empty(outbox.Batches);
        Assert.Null(profile.AttendanceIndex);
    }

    [Fact]
    public async Task Handle_DeletedHostDoesNotReadStudentsOrRecreateRuntime()
    {
        var profile = CreateProfile();
        var repository = new StudentProfileRepositoryStub([profile]);
        var runtime = new EducationSimulationRuntimeRepositoryStub();
        var handler = CreateHandler(repository, new(), new(), runtime, new(Now));
        Assert.Equal(0, await handler.Handle(CreateCommand(profile), default));
        Assert.Equal(0, repository.GetCallCount);
        Assert.Empty(runtime.Runtimes);
    }

    [Fact]
    public async Task Handle_RejectsAnotherScenario()
    {
        var profile = CreateProfile();
        var runtime = new EducationSimulationRuntimeRepositoryStub();
        runtime.Runtimes[profile.SimulationHostId] = new(new SimulationScenarioKey("other"), new SimulationHostTypeKey("other"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler(new([profile]), new(), new(), runtime).Handle(CreateCommand(profile), default));
        Assert.Null(profile.AttendanceIndex);
    }

    [Fact]
    public async Task Handle_ValidatesWholeBatchBeforeMutatingAnyStudent()
    {
        var first = CreateProfile();
        var second = CreateProfile(first.SimulationHostId.Value);
        var command = CreateCommand(first);
        command = command with { Residents = [command.Residents[0], CreateCommand(second).Residents[0] with
            { Conditions = command.Residents[0].Conditions with { Energy = -1 } }] };
        var unitOfWork = new EducationUnitOfWorkStub();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateHandler(new([first, second]), new(), unitOfWork).Handle(command, default));
        Assert.Equal(0, unitOfWork.TransactionCount);
        Assert.Null(first.AttendanceIndex);
    }

    private static StudentProfile CreateProfile(Guid? hostId = null)
    {
        var profile = StudentProfile.Register(new ResidentId(Guid.NewGuid()), new SimulationHostId(hostId ?? Guid.NewGuid()),
            new DateOnly(2030, 1, 1), true, true, 1, Now);
        profile.RecordParticipationChange();
        return profile;
    }

    private static EvaluateLearningAttendanceCommand CreateCommand(StudentProfile profile) =>
        new(profile.SimulationHostId.Value, 5, Now, [new(profile.ResidentId.Value, 0, 1,
            new(18, 100, 0, 100, false, 1m, 1m, 1m, 1m, 0m, 0m, 0m, false, true, false, 0.5m))]);

    private static EvaluateLearningAttendanceCommandHandler CreateHandler(StudentProfileRepositoryStub repository,
        AttendanceOutbox outbox, EducationUnitOfWorkStub unitOfWork,
        EducationSimulationRuntimeRepositoryStub? runtime = null, EducationSimulationDeletionRepositoryStub? deletion = null) =>
        new(repository, runtime ?? new(), deletion ?? new(), outbox, unitOfWork, new ClassicCityLearningAttendancePolicy(), TimeProvider.System);

    private sealed class AttendanceOutbox : IEducationAttendanceOutboxWriter
    {
        public List<EducationAttendanceEvaluatedBatchV1> Batches { get; } = [];
        public Task AddAsync(EducationAttendanceEvaluatedBatchV1 batch, CancellationToken cancellationToken)
        {
            Batches.Add(batch);
            return Task.CompletedTask;
        }
    }
}

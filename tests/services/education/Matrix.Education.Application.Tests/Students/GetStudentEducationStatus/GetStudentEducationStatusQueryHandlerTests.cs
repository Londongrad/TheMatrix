using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Students.GetStudentEducationStatus;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Students.GetStudentEducationStatus;

public sealed class GetStudentEducationStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_ForwardsSimulationAndResidentIdentityToReader()
    {
        Guid simulationHostId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid residentId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        var expected = new StudentEducationStatusView(
            ResidentId: residentId,
            IsAlive: true,
            IsActive: true,
            CompletedStage: "primary",
            CompletedStageOn: new DateOnly(2026, 7, 11),
            ActiveEnrollment: null);
        var reader = new StudentEducationStatusReaderStub(expected);
        var handler = new GetStudentEducationStatusQueryHandler(reader);

        StudentEducationStatusView? result = await handler.Handle(
            new GetStudentEducationStatusQuery(simulationHostId, residentId),
            CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(new SimulationHostId(simulationHostId), reader.SimulationHostId);
        Assert.Equal(new ResidentId(residentId), reader.ResidentId);
    }

    private sealed class StudentEducationStatusReaderStub(
        StudentEducationStatusView? result)
        : IStudentEducationStatusReader
    {
        public SimulationHostId? SimulationHostId { get; private set; }
        public ResidentId? ResidentId { get; private set; }

        public Task<StudentEducationStatusView?> GetAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            CancellationToken cancellationToken = default)
        {
            SimulationHostId = simulationHostId;
            ResidentId = residentId;
            return Task.FromResult(result);
        }
    }
}

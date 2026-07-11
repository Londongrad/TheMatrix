using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.Education.Integration.Consumers;
using Matrix.Education.Integration.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers;

public sealed class SimulationEducationInstitutionProvisioningConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_SendsMappedInstitutionSynchronizationCommand()
    {
        var mediator = new EducationIntegrationMediatorStub
        {
            InstitutionResult = new SynchronizeEducationInstitutionsResult(
                Status: SynchronizeEducationInstitutionsStatus.Applied,
                AddedInstitutions: 1,
                UpdatedInstitutions: 0,
                IgnoredInstitutions: 0)
        };
        var consumer = new SimulationEducationInstitutionProvisioningConsumer(
            mediator,
            NullLogger<SimulationEducationInstitutionProvisioningConsumer>.Instance);
        SimulationEducationInstitutionProvisioningBatchV1 message = CreateMessage();

        await consumer.ConsumeAsync(
            message: message,
            cancellationToken: CancellationToken.None);

        SynchronizeEducationInstitutionsCommand command = Assert.Single(mediator.InstitutionCommands);
        Assert.Equal(
            expected: message.SimulationHostId,
            actual: command.SimulationHostId);
        Assert.Equal(
            expected: message.SourceRevision,
            actual: command.SourceRevision);
        SynchronizeEducationInstitutionItem institution = Assert.Single(command.Institutions);
        Assert.Equal(
            expected: message.Institutions[0].InstitutionId,
            actual: institution.InstitutionId);
    }

    [Fact]
    public async Task ConsumeAsync_WhenSimulationWasDeleted_CompletesMessage()
    {
        var mediator = new EducationIntegrationMediatorStub
        {
            InstitutionResult = new SynchronizeEducationInstitutionsResult(
                Status: SynchronizeEducationInstitutionsStatus.SimulationDeleted,
                AddedInstitutions: 0,
                UpdatedInstitutions: 0,
                IgnoredInstitutions: 1)
        };
        var consumer = new SimulationEducationInstitutionProvisioningConsumer(
            mediator,
            NullLogger<SimulationEducationInstitutionProvisioningConsumer>.Instance);

        await consumer.ConsumeAsync(
            message: CreateMessage(),
            cancellationToken: CancellationToken.None);

        Assert.Single(mediator.InstitutionCommands);
    }

    private static SimulationEducationInstitutionProvisioningBatchV1 CreateMessage()
    {
        var institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        return new SimulationEducationInstitutionProvisioningBatchV1(
            SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SourceRevision: 0,
            SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
            CorrelationId: "education-institutions:0",
            BatchNumber: 1,
            TotalBatches: 1,
            Institutions:
            [
                new SimulationEducationInstitutionProvisioningV1(
                    InstitutionId: institutionId,
                    Name: "Central Education Complex",
                    Kind: "School",
                    LocationAnchorId: institutionId,
                    Capacity: 640,
                    IsActive: true)
            ]);
    }
}

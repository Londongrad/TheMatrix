using Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers;

public sealed class SimulationCareFacilityProvisioningConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_SendsMappedFacilitySynchronizationCommand()
    {
        var mediator = new HealthcareIntegrationMediatorStub
        {
            FacilityResult = new SynchronizeCareFacilitiesResult(
                Status: SynchronizeCareFacilitiesStatus.Applied,
                AddedFacilities: 1,
                UpdatedFacilities: 0,
                IgnoredFacilities: 0)
        };
        var consumer = new SimulationCareFacilityProvisioningConsumer(
            mediator,
            NullLogger<SimulationCareFacilityProvisioningConsumer>.Instance);
        SimulationCareFacilityProvisioningBatchV1 message = CreateMessage();

        await consumer.ConsumeAsync(message, CancellationToken.None);

        SynchronizeCareFacilitiesCommand command = Assert.Single(mediator.FacilityCommands);
        Assert.Equal(message.SimulationHostId, command.SimulationHostId);
        Assert.Equal(message.SourceRevision, command.SourceRevision);
        SynchronizeCareFacilityItem facility = Assert.Single(command.Facilities);
        Assert.Equal(message.Facilities[0].FacilityId, facility.FacilityId);
    }

    [Fact]
    public async Task ConsumeAsync_WhenSimulationWasDeleted_StillCompletesMessage()
    {
        var mediator = new HealthcareIntegrationMediatorStub
        {
            FacilityResult = new SynchronizeCareFacilitiesResult(
                Status: SynchronizeCareFacilitiesStatus.SimulationDeleted,
                AddedFacilities: 0,
                UpdatedFacilities: 0,
                IgnoredFacilities: 1)
        };
        var consumer = new SimulationCareFacilityProvisioningConsumer(
            mediator,
            NullLogger<SimulationCareFacilityProvisioningConsumer>.Instance);

        await consumer.ConsumeAsync(CreateMessage(), CancellationToken.None);

        Assert.Single(mediator.FacilityCommands);
    }

    private static SimulationCareFacilityProvisioningBatchV1 CreateMessage()
    {
        Guid facilityId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        return new SimulationCareFacilityProvisioningBatchV1(
            SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SourceRevision: 0,
            SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
            CorrelationId: "care-facilities:0",
            BatchNumber: 1,
            TotalBatches: 1,
            Facilities:
            [
                new SimulationCareFacilityProvisioningV1(
                    FacilityId: facilityId,
                    Name: "Central Hospital",
                    Kind: "Hospital",
                    LocationAnchorId: facilityId,
                    DailyPatientCapacity: 240,
                    IsActive: true)
            ]);
    }
}

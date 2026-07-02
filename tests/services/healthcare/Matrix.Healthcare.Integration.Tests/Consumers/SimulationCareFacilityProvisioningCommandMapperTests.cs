using Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.SimulationCore.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers;

public sealed class SimulationCareFacilityProvisioningCommandMapperTests
{
    [Fact]
    public void Map_ValidBatch_MapsFacilityProvisioning()
    {
        SimulationCareFacilityProvisioningBatchV1 message = CreateMessage();

        SynchronizeCareFacilitiesCommand command =
            SimulationCareFacilityProvisioningCommandMapper.Map(message);

        Assert.Equal(message.SimulationHostId, command.SimulationHostId);
        Assert.Equal(message.SourceRevision, command.SourceRevision);
        Assert.Equal(message.SynchronizedAtUtc, command.SynchronizedAtUtc);
        SynchronizeCareFacilityItem facility = Assert.Single(command.Facilities);
        SimulationCareFacilityProvisioningV1 source = Assert.Single(message.Facilities);
        Assert.Equal(source.FacilityId, facility.FacilityId);
        Assert.Equal(source.Name, facility.Name);
        Assert.Equal(source.Kind, facility.Kind);
        Assert.Equal(source.LocationAnchorId, facility.LocationAnchorId);
        Assert.Equal(source.DailyPatientCapacity, facility.DailyPatientCapacity);
        Assert.Equal(source.IsActive, facility.IsActive);
    }

    [Fact]
    public void Map_MissingCorrelationId_ThrowsArgumentException()
    {
        SimulationCareFacilityProvisioningBatchV1 message = CreateMessage() with
        {
            CorrelationId = " "
        };

        Assert.Throws<ArgumentException>(() =>
            SimulationCareFacilityProvisioningCommandMapper.Map(message));
    }

    [Fact]
    public void Map_InvalidBatchPosition_ThrowsArgumentException()
    {
        SimulationCareFacilityProvisioningBatchV1 message = CreateMessage() with
        {
            BatchNumber = 2,
            TotalBatches = 1
        };

        Assert.Throws<ArgumentException>(() =>
            SimulationCareFacilityProvisioningCommandMapper.Map(message));
    }

    private static SimulationCareFacilityProvisioningBatchV1 CreateMessage()
    {
        Guid facilityId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        return new SimulationCareFacilityProvisioningBatchV1(
            SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SourceRevision: 4,
            SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
            CorrelationId: "care-facilities:4",
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

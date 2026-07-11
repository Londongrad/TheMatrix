using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.Education.Integration.Consumers;
using Matrix.SimulationCore.Contracts.Events;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers;

public sealed class SimulationEducationInstitutionProvisioningCommandMapperTests
{
    [Fact]
    public void Map_ValidBatch_MapsInstitutionProvisioning()
    {
        SimulationEducationInstitutionProvisioningBatchV1 message = CreateMessage();

        SynchronizeEducationInstitutionsCommand command =
            SimulationEducationInstitutionProvisioningCommandMapper.Map(message);

        Assert.Equal(
            expected: message.SimulationHostId,
            actual: command.SimulationHostId);
        Assert.Equal(
            expected: message.SourceRevision,
            actual: command.SourceRevision);
        Assert.Equal(
            expected: message.SynchronizedAtUtc,
            actual: command.SynchronizedAtUtc);
        SynchronizeEducationInstitutionItem institution = Assert.Single(command.Institutions);
        SimulationEducationInstitutionProvisioningV1 source = Assert.Single(message.Institutions);
        Assert.Equal(
            expected: source.InstitutionId,
            actual: institution.InstitutionId);
        Assert.Equal(
            expected: source.Name,
            actual: institution.Name);
        Assert.Equal(
            expected: source.Kind,
            actual: institution.Kind);
        Assert.Equal(
            expected: source.LocationAnchorId,
            actual: institution.LocationAnchorId);
        Assert.Equal(
            expected: source.Capacity,
            actual: institution.Capacity);
        Assert.Equal(
            expected: source.IsActive,
            actual: institution.IsActive);
    }

    [Fact]
    public void Map_MissingCorrelationId_ThrowsArgumentException()
    {
        SimulationEducationInstitutionProvisioningBatchV1 message = CreateMessage() with
        {
            CorrelationId = " "
        };

        Assert.Throws<ArgumentException>(() =>
            SimulationEducationInstitutionProvisioningCommandMapper.Map(message));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(1, 0)]
    public void Map_InvalidBatchPosition_ThrowsArgumentException(
        int batchNumber,
        int totalBatches)
    {
        SimulationEducationInstitutionProvisioningBatchV1 message = CreateMessage() with
        {
            BatchNumber = batchNumber,
            TotalBatches = totalBatches
        };

        Assert.Throws<ArgumentException>(() =>
            SimulationEducationInstitutionProvisioningCommandMapper.Map(message));
    }

    private static SimulationEducationInstitutionProvisioningBatchV1 CreateMessage()
    {
        var institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        return new SimulationEducationInstitutionProvisioningBatchV1(
            SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SourceRevision: 4,
            SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
            CorrelationId: "education-institutions:4",
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

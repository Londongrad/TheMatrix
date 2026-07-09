using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class HealthcarePopulationHealthSnapshotCommandMapperTests
{
    [Fact]
    public void Map_PreservesHealthcareOwnedAggregate()
    {
        var messageId = Guid.NewGuid();
        var districtId = Guid.NewGuid();
        var message = new HealthcarePopulationHealthSnapshotV1(
            SimulationHostId: Guid.NewGuid(),
            SourceRevision: 17,
            CurrentDate: new DateOnly(2048, 5, 6),
            PatientCount: 100,
            ActiveIllnessCount: 8,
            SevereIllnessCount: 2,
            MedicalLoadIndex: 0.82m,
            TriagePressureIndex: 0.34m,
            RecoverySupportIndex: 1.12m,
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
            CorrelationId: "health-risk:17:population-health",
            Communities:
            [
                new HealthcareCommunityHealthSnapshotV1(
                    CommunityId: districtId,
                    PatientCount: 40,
                    ActiveIllnessCount: 5,
                    SevereIllnessCount: 1)
            ]);

        ApplyHealthcarePressureSnapshotCommand command =
            HealthcarePopulationHealthSnapshotCommandMapper.Map(
                message,
                messageId,
                HealthcarePopulationHealthSnapshotConsumerDefinition.EndpointNameValue);

        Assert.Equal(message.SimulationHostId, command.CityId);
        Assert.Equal(messageId, command.IntegrationMessageId);
        Assert.Equal(17, command.SourceRevision);
        Assert.Equal(100, command.PatientCount);
        Assert.Equal(8, command.ActiveIllnessCount);
        Assert.Equal(2, command.SevereIllnessCount);
        Assert.Equal(0.82m, command.MedicalLoadIndex);
        HealthcareDistrictHealthSnapshotInput district = Assert.Single(command.Districts);
        Assert.Equal(districtId, district.DistrictId);
        Assert.Equal(40, district.PatientCount);
        Assert.Equal(5, district.ActiveIllnessCount);
        Assert.Equal(1, district.SevereIllnessCount);
    }

    [Fact]
    public void ConsumerDefinition_SerializesProjectionUpdates()
    {
        Assert.Equal(
            "population-healthcare-population-health-snapshot-v1",
            HealthcarePopulationHealthSnapshotConsumerDefinition.EndpointNameValue);
        Assert.Equal(1, HealthcarePopulationHealthSnapshotConsumerDefinition.ConcurrentMessageLimitValue);
    }
}

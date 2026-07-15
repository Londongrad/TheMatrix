using Matrix.Education.Contracts.Events;
using Matrix.Population.Application.Integration.Education.ApplyEducationParticipation;
using Matrix.Population.Infrastructure.Consumers.Education;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Consumers.Education
{
    public sealed class EducationStudentParticipationCommandMapperTests
    {
        [Fact]
        public void Map_ParticipationBatch_PreservesEnvelopeAndStudentState()
        {
            Guid messageId = Guid.NewGuid();
            Guid residentId = Guid.NewGuid();
            var message = new EducationStudentParticipationBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SnapshotDate: new DateOnly(2048, 5, 3),
                OccurredAtUtc: new DateTimeOffset(2048, 5, 3, 8, 0, 0, TimeSpan.Zero),
                CorrelationId: "education:progression:42",
                BatchNumber: 2,
                TotalBatches: 3,
                Students:
                [
                    new EducationStudentParticipationV1(
                        ResidentId: residentId,
                        ParticipationRevision: 4,
                        ResidentLifecycleRevision: 2,
                        IsEnrolled: true,
                        ActiveStage: "lower-secondary",
                        InstitutionId: Guid.NewGuid(),
                        InstitutionAnchorId: Guid.NewGuid(),
                        EnrolledOn: new DateOnly(2048, 5, 1),
                        CompletedStage: "primary",
                        CompletedStageOn: new DateOnly(2048, 4, 30))
                ]);

            ApplyEducationParticipationCommand command =
                EducationStudentParticipationCommandMapper.Map(
                    message,
                    messageId,
                    EducationStudentParticipationConsumerDefinition.EndpointNameValue);

            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            Assert.Equal(messageId, command.IntegrationMessageId);
            Assert.Equal(2, command.BatchNumber);
            Assert.Equal(3, command.TotalBatches);
            StudentEducationParticipationInput student = Assert.Single(command.Students);
            Assert.Equal(residentId, student.ResidentId);
            Assert.Equal(4, student.ParticipationRevision);
            Assert.Equal("lower-secondary", student.ActiveStage);
            Assert.Equal("primary", student.CompletedStage);
        }

        [Fact]
        public void ConsumerDefinition_ParallelizesHostsWithBoundedConcurrency()
        {
            Assert.Equal(
                "population-education-participation-v1",
                EducationStudentParticipationConsumerDefinition.EndpointNameValue);
            Assert.Equal(8, EducationStudentParticipationConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}

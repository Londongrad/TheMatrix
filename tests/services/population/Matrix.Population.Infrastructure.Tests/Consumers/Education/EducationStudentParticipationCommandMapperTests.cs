using Matrix.Education.Contracts.Events;
using System.Text.Json;
using Matrix.Population.Application.Integration.Education.ApplyEducationParticipation;
using Matrix.Population.Infrastructure.Consumers.Education;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Consumers.Education
{
    public sealed class EducationStudentParticipationCommandMapperTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Map_ParticipationBatch_PreservesEnvelopeAndStudentState(bool hasEconomics)
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
                        CompletedStageOn: new DateOnly(2048, 4, 30),
                        EconomicEffects: hasEconomics ? new([new(0, 99m)], 8m, 0.1d, 0.5d, -0.1m, 0.05m, 0.05m) : null)
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
            if (hasEconomics)
            {
                Assert.NotNull(student.Economics);
                Assert.Equal(99m, student.Economics.TransferIncome.Resolve(18));
                Assert.Equal(0.5d, student.Economics.EmploymentAvailabilityFactor);
            }
            else
                Assert.Null(student.Economics);
        }

        [Fact]
        public void ConsumerDefinition_ParallelizesHostsWithBoundedConcurrency()
        {
            Assert.Equal(
                "population-education-participation-v1",
                EducationStudentParticipationConsumerDefinition.EndpointNameValue);
            Assert.Equal(8, EducationStudentParticipationConsumerDefinition.ConcurrentMessageLimitValue);
        }

        [Fact]
        public void LegacyJsonWithoutEconomicEffects_RemainsReadable()
        {
            var student = JsonSerializer.Deserialize<EducationStudentParticipationV1>("""
                {"ResidentId":"11111111-1111-1111-1111-111111111111","ParticipationRevision":1,
                "ResidentLifecycleRevision":0,"IsEnrolled":false,"ActiveStage":null,
                "InstitutionId":null,"InstitutionAnchorId":null,"EnrolledOn":null,
                "CompletedStage":"primary","CompletedStageOn":"2048-01-01"}
                """)!;
            var message = new EducationStudentParticipationBatchV1(Guid.NewGuid(), new DateOnly(2048, 5, 3),
                DateTimeOffset.UtcNow, "legacy", 1, 1, [student]);
            var command = EducationStudentParticipationCommandMapper.Map(message, Guid.NewGuid(), "test");
            Assert.Null(Assert.Single(command.Students).Economics);
            Assert.Null(command.Students[0].Routine);
            Assert.Equal("primary", command.Students[0].CompletedStage);
        }

        [Fact]
        public void Map_PreservesAndSharesEqualRoutinesWithinBatch()
        {
            var first = new EducationStudentParticipationV1(Guid.NewGuid(), 1, 0, true, "higher",
                Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2048, 1, 1), null, null,
                DailyRoutine: new(new(600, 1020, 65, "demanding")));
            var message = new EducationStudentParticipationBatchV1(Guid.NewGuid(), new DateOnly(2048, 5, 3),
                DateTimeOffset.UtcNow, "routine", 1, 1,
                [first, first with { ResidentId = Guid.NewGuid(), DailyRoutine = new(new(600, 1020, 65, "demanding")) }]);
            var command = EducationStudentParticipationCommandMapper.Map(message, Guid.NewGuid(), "test");
            Assert.NotNull(command.Students[0].Routine);
            Assert.Same(command.Students[0].Routine, command.Students[1].Routine);
            Assert.Equal(TimeSpan.FromHours(10), command.Students[0].Routine!.StructuredActivityStart);
            Assert.Throws<ArgumentException>(() => EducationStudentParticipationCommandMapper.Map(message with
                { Students = [first, first with { DailyRoutine = new(new(600, 590, 65, "moderate")) }] }, Guid.NewGuid(), "test"));
        }
    }
}

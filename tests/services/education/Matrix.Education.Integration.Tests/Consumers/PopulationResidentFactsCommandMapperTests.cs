using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsCommandMapperTests
    {
        [Fact]
        public void Map_TransfersEducationRelevantFactsAndBatchRevision()
        {
            var hostId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var firstResidentId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
            var secondResidentId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
            DateTimeOffset synchronizedAtUtc = DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
            var message = new PopulationResidentFactsBatchV1(
                SimulationHostId: hostId,
                SourceRevision: 73,
                SynchronizedAtUtc: synchronizedAtUtc,
                CorrelationId: "population:host:tick:73:resident-facts",
                BatchNumber: 2,
                TotalBatches: 4,
                Residents:
                [
                    new PopulationResidentFactsV1(
                        ResidentId: firstResidentId,
                        BirthDate: new DateOnly(2020, 5, 6),
                        Sex: "Female",
                        IsAlive: true,
                        IsActive: true,
                        LifecycleRevision: 3),
                    new PopulationResidentFactsV1(
                        ResidentId: secondResidentId,
                        BirthDate: new DateOnly(2018, 4, 2),
                        Sex: "Male",
                        IsAlive: false,
                        IsActive: false,
                        LifecycleRevision: 4)
                ]);

            SynchronizeStudentProfilesCommand command = PopulationResidentFactsCommandMapper.Map(message);

            Assert.Equal(
                expected: hostId,
                actual: command.SimulationHostId);
            Assert.Equal(
                expected: synchronizedAtUtc,
                actual: command.SynchronizedAtUtc);
            Assert.Collection(
                command.Profiles,
                first => Assert.Equal(
                    expected: new SynchronizeStudentProfileItem(
                        ResidentId: firstResidentId,
                        BirthDate: new DateOnly(2020, 5, 6),
                        IsAlive: true,
                        IsActive: true,
                        SourceRevision: 73,
                        LifecycleRevision: 3),
                    actual: first),
                second => Assert.Equal(
                    expected: new SynchronizeStudentProfileItem(
                        ResidentId: secondResidentId,
                        BirthDate: new DateOnly(2018, 4, 2),
                        IsAlive: false,
                        IsActive: false,
                        SourceRevision: 73,
                        LifecycleRevision: 4),
                    actual: second));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(2, 1)]
        [InlineData(1, 0)]
        public void Map_WhenBatchPositionIsInvalid_ThrowsArgumentException(
            int batchNumber,
            int totalBatches)
        {
            PopulationResidentFactsBatchV1 message = CreateMessage(
                batchNumber: batchNumber,
                totalBatches: totalBatches);

            Assert.Throws<ArgumentException>(() => PopulationResidentFactsCommandMapper.Map(message));
        }

        [Fact]
        public void Map_WhenCorrelationIdIsMissing_ThrowsArgumentException()
        {
            PopulationResidentFactsBatchV1 message = CreateMessage() with
            {
                CorrelationId = " "
            };

            Assert.Throws<ArgumentException>(() => PopulationResidentFactsCommandMapper.Map(message));
        }

        private static PopulationResidentFactsBatchV1 CreateMessage(
            int batchNumber = 1,
            int totalBatches = 1)
        {
            return new PopulationResidentFactsBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 1,
                SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "resident-facts",
                BatchNumber: batchNumber,
                TotalBatches: totalBatches,
                Residents:
                [
                    new PopulationResidentFactsV1(
                        ResidentId: Guid.NewGuid(),
                        BirthDate: new DateOnly(2020, 5, 6),
                        Sex: "Female",
                        IsAlive: true,
                        IsActive: true)
                ]);
        }
    }
}

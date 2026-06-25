using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsCommandMapperTests
    {
        [Fact]
        public void Map_ValidBatch_MapsDemographicsAndSourceRevision()
        {
            PopulationResidentFactsBatchV1 message = CreateMessage(
                new PopulationResidentFactsV1(
                    ResidentId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                    BirthDate: new DateOnly(2027, 4, 3),
                    Sex: "female",
                    IsAlive: true,
                    IsActive: false));

            SynchronizePatientProfilesCommand command =
                PopulationResidentFactsCommandMapper.Map(message);

            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            Assert.Equal(message.SynchronizedAtUtc, command.SynchronizedAtUtc);
            SynchronizePatientProfileItem profile = Assert.Single(command.Profiles);
            Assert.Equal(message.Residents[0].ResidentId, profile.PatientId);
            Assert.Equal(message.Residents[0].BirthDate, profile.BirthDate);
            Assert.Equal(PatientSex.Female, profile.Sex);
            Assert.True(profile.IsAlive);
            Assert.False(profile.IsActive);
            Assert.Equal(message.SourceRevision, profile.SourceRevision);
        }

        [Fact]
        public void Map_UnsupportedSex_ThrowsArgumentException()
        {
            PopulationResidentFactsBatchV1 message = CreateMessage(
                new PopulationResidentFactsV1(
                    ResidentId: Guid.NewGuid(),
                    BirthDate: new DateOnly(2027, 4, 3),
                    Sex: "unknown",
                    IsAlive: true,
                    IsActive: true));

            Assert.Throws<ArgumentException>(() =>
                PopulationResidentFactsCommandMapper.Map(message));
        }

        [Fact]
        public void Map_InvalidBatchPosition_ThrowsArgumentException()
        {
            PopulationResidentFactsBatchV1 valid = CreateMessage(
                new PopulationResidentFactsV1(
                    ResidentId: Guid.NewGuid(),
                    BirthDate: new DateOnly(2027, 4, 3),
                    Sex: "Male",
                    IsAlive: true,
                    IsActive: true));
            PopulationResidentFactsBatchV1 invalid = valid with
            {
                BatchNumber = 2,
                TotalBatches = 1
            };

            Assert.Throws<ArgumentException>(() =>
                PopulationResidentFactsCommandMapper.Map(invalid));
        }

        private static PopulationResidentFactsBatchV1 CreateMessage(
            params PopulationResidentFactsV1[] residents)
        {
            return new PopulationResidentFactsBatchV1(
                SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                SourceRevision: 14,
                SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "resident-facts:14",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents: residents);
        }
    }
}

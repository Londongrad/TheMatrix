using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentMedicalStateCommandMapperTests
    {
        [Fact]
        public void Map_LegacyMedicalState_MapsOnlyUniversalVitalState()
        {
            Guid patientId = Guid.NewGuid();
            DateOnly diagnosedOn = new(2048, 5, 6);
            PopulationResidentMedicalStateBatchV1 message = CreateMessage(
                new PopulationResidentMedicalStateV1(
                    ResidentId: patientId,
                    HealthScore: 63,
                    CurrentIllnessKind: "infection",
                    CurrentIllnessSeverity: "MODERATE",
                    DiagnosedOn: diagnosedOn,
                    LastRecoveredOn: new DateOnly(2048, 4, 20),
                    LifecycleRevision: 4));

            InitializePatientMedicalRecordsCommand command =
                PopulationResidentMedicalStateCommandMapper.Map(message);

            InitializePatientMedicalRecordItem record = Assert.Single(command.Records);
            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            Assert.Equal(message.ObservedAtUtc, command.ObservedAtUtc);
            Assert.Equal(message.SourceRevision, command.SourceRevision);
            Assert.Equal(patientId, record.PatientId);
            Assert.Equal(63, record.HealthScore);
            Assert.Equal(4, record.LifecycleRevision);
        }

        private static PopulationResidentMedicalStateBatchV1 CreateMessage(
            params PopulationResidentMedicalStateV1[] residents)
        {
            return new PopulationResidentMedicalStateBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 9,
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "medical-state-9",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents: residents);
        }
    }
}

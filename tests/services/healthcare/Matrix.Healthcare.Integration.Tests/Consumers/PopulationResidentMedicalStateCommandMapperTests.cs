using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentMedicalStateCommandMapperTests
    {
        [Fact]
        public void Map_ActiveIllness_MapsHealthcareMedicalPrimitives()
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
            Assert.Equal(IllnessKind.Infection, record.CurrentIllnessKind);
            Assert.Equal(IllnessSeverity.Moderate, record.CurrentIllnessSeverity);
            Assert.Equal(diagnosedOn, record.DiagnosedOn);
            Assert.Equal(4, record.LifecycleRevision);
        }

        [Fact]
        public void Map_HealthyResident_PreservesNullActiveIllnessValues()
        {
            PopulationResidentMedicalStateBatchV1 message = CreateMessage(
                new PopulationResidentMedicalStateV1(
                    ResidentId: Guid.NewGuid(),
                    HealthScore: 100,
                    CurrentIllnessKind: null,
                    CurrentIllnessSeverity: null,
                    DiagnosedOn: null,
                    LastRecoveredOn: null));

            InitializePatientMedicalRecordItem record = Assert.Single(
                PopulationResidentMedicalStateCommandMapper.Map(message).Records);

            Assert.Null(record.CurrentIllnessKind);
            Assert.Null(record.CurrentIllnessSeverity);
        }

        [Fact]
        public void Map_UnsupportedIllnessKind_ThrowsArgumentException()
        {
            PopulationResidentMedicalStateBatchV1 message = CreateMessage(
                new PopulationResidentMedicalStateV1(
                    ResidentId: Guid.NewGuid(),
                    HealthScore: 80,
                    CurrentIllnessKind: "Unknown",
                    CurrentIllnessSeverity: "Mild",
                    DiagnosedOn: new DateOnly(2048, 5, 6),
                    LastRecoveredOn: null));

            Assert.Throws<ArgumentException>(() =>
                PopulationResidentMedicalStateCommandMapper.Map(message));
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

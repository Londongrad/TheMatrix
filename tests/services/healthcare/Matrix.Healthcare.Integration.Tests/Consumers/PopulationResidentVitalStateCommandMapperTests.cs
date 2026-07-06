using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentVitalStateCommandMapperTests
    {
        [Fact]
        public void Map_ResidentVitalState_MapsPatientInitializationInput()
        {
            Guid patientId = Guid.NewGuid();
            var message = new PopulationResidentVitalStateBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 9,
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "vital-state-9",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentVitalStateV1(
                        ResidentId: patientId,
                        HealthScore: 63,
                        LifecycleRevision: 4)
                ]);

            InitializePatientMedicalRecordsCommand command =
                PopulationResidentVitalStateCommandMapper.Map(message);

            InitializePatientMedicalRecordItem record = Assert.Single(command.Records);
            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            Assert.Equal(message.ObservedAtUtc, command.ObservedAtUtc);
            Assert.Equal(message.SourceRevision, command.SourceRevision);
            Assert.Equal(patientId, record.PatientId);
            Assert.Equal(63, record.HealthScore);
            Assert.Equal(4, record.LifecycleRevision);
        }
    }
}

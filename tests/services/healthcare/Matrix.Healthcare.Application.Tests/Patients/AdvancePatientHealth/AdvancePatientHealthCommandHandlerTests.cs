using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Patients.AdvancePatientHealth
{
    public sealed class AdvancePatientHealthCommandHandlerTests
    {
        private static readonly Guid HostId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid PatientGuid = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        private static readonly DateOnly CurrentDate = new(2048, 5, 6);

        [Fact]
        public async Task Handle_EligiblePatient_AdvancesOnceAndWritesOutcomeAtomically()
        {
            PatientMedicalRecord record = CreateMedicalRecord(health: 2);
            var medicalRepository = new MedicalRecordRepositoryStub([record]);
            var outboxWriter = new OutcomeOutboxWriterStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                unitOfWork: unitOfWork);

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17),
                CancellationToken.None);

            PatientHealthProgressionResultItem outcome = Assert.Single(result.Outcomes);
            Assert.Equal(AdvancePatientHealthStatus.Applied, result.Status);
            Assert.Equal(1, result.ProcessedPatients);
            Assert.Equal(0, result.StalePatients);
            Assert.Equal(-2, outcome.HealthDelta);
            Assert.True(outcome.BecameCritical);
            Assert.Equal(0, record.Health.Value);
            Assert.Equal(17, record.LastProgressionRevision);
            PatientHealthOutcomeBatch outboxBatch = Assert.Single(outboxWriter.Batches);
            Assert.Equal(17, outboxBatch.SourceRevision);
            Assert.Equal(CurrentDate, outboxBatch.CurrentDate);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
            Assert.Equal(1, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DuplicateRevision_DoesNotApplyOrPublishAgain()
        {
            PatientMedicalRecord record = CreateMedicalRecord(health: 70);
            record.TryAcceptProgressionRevision(17);
            var medicalRepository = new MedicalRecordRepositoryStub([record]);
            var outboxWriter = new OutcomeOutboxWriterStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                unitOfWork: unitOfWork);

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17),
                CancellationToken.None);

            Assert.Equal(0, result.ProcessedPatients);
            Assert.Equal(1, result.StalePatients);
            Assert.Empty(result.Outcomes);
            Assert.Empty(outboxWriter.Batches);
            Assert.Equal(0, unitOfWork.SaveCount);
            Assert.Equal(70, record.Health.Value);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_DoesNotLoadPatientData()
        {
            var medicalRepository = new MedicalRecordRepositoryStub();
            var outboxWriter = new OutcomeOutboxWriterStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                deletedAtUtc: DateTimeOffset.Parse("2048-05-06T10:01:00+00:00"));

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17),
                CancellationToken.None);

            Assert.Equal(AdvancePatientHealthStatus.SimulationDeleted, result.Status);
            Assert.Equal(0, medicalRepository.GetCallCount);
            Assert.Empty(outboxWriter.Batches);
        }

        private static AdvancePatientHealthCommandHandler CreateHandler(
            MedicalRecordRepositoryStub medicalRepository,
            OutcomeOutboxWriterStub outboxWriter,
            DateTimeOffset? deletedAtUtc = null,
            HealthcareUnitOfWorkStub? unitOfWork = null)
        {
            return new AdvancePatientHealthCommandHandler(
                patientProfileRepository: new PatientProfileRepositoryStub([CreateProfile()]),
                medicalRecordRepository: medicalRepository,
                deletionRepository: new HealthcareSimulationDeletionRepositoryStub(deletedAtUtc),
                outcomeOutboxWriter: outboxWriter,
                progressionPolicy: CreatePolicy(),
                unitOfWork: unitOfWork ?? new HealthcareUnitOfWorkStub());
        }

        private static AdvancePatientHealthCommand CreateCommand(long sourceRevision)
        {
            return new AdvancePatientHealthCommand(
                SimulationHostId: HostId,
                SourceRevision: sourceRevision,
                PreviousDate: CurrentDate.AddDays(-1),
                CurrentDate: CurrentDate,
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: $"health-risk:{sourceRevision}",
                BatchNumber: 1,
                TotalBatches: 1,
                Patients:
                [
                    new AdvancePatientHealthRiskItem(
                        PatientId: PatientGuid,
                        EnergyScore: 5,
                        HappinessScore: 10,
                        StressScore: 95,
                        SocialNeedScore: 90,
                        IsVulnerable: true,
                        HousingStability: PatientHousingStability.Unhoused,
                        HasStructuredDailyActivity: false,
                        InfectiousHouseholdContacts: 1,
                        HouseholdSize: 2,
                        CaregiverSupportStrength: 0d,
                        HadAdverseWeatherExposure: true,
                        HealthcareSupportStrength: 0d,
                        PublicHealthRiskStrength: 1d)
                ]);
        }

        private static PatientProfile CreateProfile()
        {
            return PatientProfile.Register(
                new PatientId(PatientGuid),
                new SimulationHostId(HostId),
                birthDate: new DateOnly(2030, 5, 6),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: 16,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T09:59:00+00:00"));
        }

        private static PatientMedicalRecord CreateMedicalRecord(int health)
        {
            return PatientMedicalRecord.Register(
                new PatientId(PatientGuid),
                new SimulationHostId(HostId),
                new HealthScore(health),
                PatientIllnessState.Active(
                    IllnessKind.Infection,
                    IllnessSeverity.Severe,
                    CurrentDate.AddDays(-3)));
        }

        private static PatientIllnessProgressionPolicy CreatePolicy()
        {
            var riskRoll = new PatientMedicalRiskRoll();
            return new PatientIllnessProgressionPolicy(
                new PatientIllnessDiagnosisPolicy(riskRoll),
                new PatientIllnessCoursePolicy(riskRoll),
                new PatientIllnessBurdenPolicy());
        }

        private sealed class MedicalRecordRepositoryStub(
            IReadOnlyList<PatientMedicalRecord>? records = null)
            : IPatientMedicalRecordRepository
        {
            private readonly IReadOnlyList<PatientMedicalRecord> _records =
                records ?? Array.Empty<PatientMedicalRecord>();

            internal int GetCallCount { get; private set; }

            public Task<IReadOnlyList<PatientMedicalRecord>> GetByIdsAsync(
                IReadOnlyCollection<PatientId> patientIds,
                CancellationToken cancellationToken = default)
            {
                GetCallCount++;
                return Task.FromResult(_records);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<PatientMedicalRecord> recordsToAdd,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class OutcomeOutboxWriterStub : IPatientHealthOutcomeOutboxWriter
        {
            internal List<PatientHealthOutcomeBatch> Batches { get; } = [];

            public Task AddAsync(
                PatientHealthOutcomeBatch batch,
                CancellationToken cancellationToken = default)
            {
                Batches.Add(batch);
                return Task.CompletedTask;
            }
        }
    }
}

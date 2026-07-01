using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Patients.InitializePatientMedicalRecords
{
    public sealed class InitializePatientMedicalRecordsCommandHandlerTests
    {
        private static readonly Guid HostId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTimeOffset ObservedAtUtc =
            new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Handle_NewRecords_PreservesTransferredMedicalStateInSingleSave()
        {
            var repository = new MedicalRecordRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new InitializePatientMedicalRecordsCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(),
                unitOfWork);
            DateOnly diagnosedOn = new(2048, 5, 3);

            InitializePatientMedicalRecordsResult result = await handler.Handle(
                new InitializePatientMedicalRecordsCommand(
                    HostId,
                    ObservedAtUtc,
                    [
                        new InitializePatientMedicalRecordItem(
                            PatientId: Guid.NewGuid(),
                            HealthScore: 61,
                            CurrentIllnessKind: IllnessKind.Infection,
                            CurrentIllnessSeverity: IllnessSeverity.Moderate,
                            DiagnosedOn: diagnosedOn,
                            LastRecoveredOn: new DateOnly(2048, 4, 20))
                    ]),
                CancellationToken.None);

            PatientMedicalRecord added = Assert.Single(repository.AddedRecords);
            Assert.Equal(InitializePatientMedicalRecordsStatus.Applied, result.Status);
            Assert.Equal(1, result.AddedRecords);
            Assert.Equal(61, added.Health.Value);
            Assert.Equal(IllnessKind.Infection, added.Illness.CurrentKind);
            Assert.Equal(diagnosedOn, added.Illness.DiagnosedOn);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Fact]
        public async Task Handle_ExistingRecord_DoesNotOverwriteHealthcareOwnedState()
        {
            Guid patientId = Guid.NewGuid();
            PatientMedicalRecord existing = PatientMedicalRecord.Register(
                new PatientId(patientId),
                new SimulationHostId(HostId),
                new HealthScore(42),
                PatientIllnessState.Healthy());
            var repository = new MedicalRecordRepositoryStub([existing]);
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new InitializePatientMedicalRecordsCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(),
                unitOfWork);

            InitializePatientMedicalRecordsResult result = await handler.Handle(
                CreateHealthyCommand(patientId, healthScore: 95),
                CancellationToken.None);

            Assert.Equal(0, result.AddedRecords);
            Assert.Equal(1, result.IgnoredRecords);
            Assert.Equal(42, existing.Health.Value);
            Assert.Empty(repository.AddedRecords);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_ExistingRecordWithNewLifecycle_ReplacesOperatorMedicalState()
        {
            Guid patientId = Guid.NewGuid();
            PatientMedicalRecord existing = PatientMedicalRecord.Register(
                new PatientId(patientId),
                new SimulationHostId(HostId),
                new HealthScore(42),
                PatientIllnessState.Active(
                    IllnessKind.Infection,
                    IllnessSeverity.Severe,
                    new DateOnly(2048, 5, 1)));
            var repository = new MedicalRecordRepositoryStub([existing]);
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new InitializePatientMedicalRecordsCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(),
                unitOfWork);

            InitializePatientMedicalRecordsResult result = await handler.Handle(
                CreateHealthyCommand(
                    patientId,
                    healthScore: 100,
                    sourceRevision: 8,
                    lifecycleRevision: 1),
                CancellationToken.None);

            Assert.Equal(0, result.AddedRecords);
            Assert.Equal(1, result.UpdatedRecords);
            Assert.Equal(0, result.IgnoredRecords);
            Assert.Equal(100, existing.Health.Value);
            Assert.False(existing.HasActiveIllness);
            Assert.Equal(1, existing.LastLifecycleRevision);
            Assert.Equal(8, existing.LastProgressionRevision);
            Assert.Equal(1, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_IgnoresLateMedicalState()
        {
            var repository = new MedicalRecordRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new InitializePatientMedicalRecordsCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(ObservedAtUtc.AddMinutes(1)),
                unitOfWork);

            InitializePatientMedicalRecordsResult result = await handler.Handle(
                CreateHealthyCommand(Guid.NewGuid(), healthScore: 100),
                CancellationToken.None);

            Assert.Equal(InitializePatientMedicalRecordsStatus.SimulationDeleted, result.Status);
            Assert.Equal(0, repository.GetCallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        private static InitializePatientMedicalRecordsCommand CreateHealthyCommand(
            Guid patientId,
            int healthScore,
            long sourceRevision = 0,
            long lifecycleRevision = 0)
        {
            return new InitializePatientMedicalRecordsCommand(
                HostId,
                ObservedAtUtc,
                [
                    new InitializePatientMedicalRecordItem(
                        PatientId: patientId,
                        HealthScore: healthScore,
                        CurrentIllnessKind: null,
                        CurrentIllnessSeverity: null,
                        DiagnosedOn: null,
                        LastRecoveredOn: null,
                        LifecycleRevision: lifecycleRevision)
                ],
                SourceRevision: sourceRevision);
        }

        private sealed class MedicalRecordRepositoryStub(
            IReadOnlyList<PatientMedicalRecord>? existingRecords = null)
            : IPatientMedicalRecordRepository
        {
            private readonly IReadOnlyList<PatientMedicalRecord> _existingRecords =
                existingRecords ?? Array.Empty<PatientMedicalRecord>();

            internal int GetCallCount { get; private set; }
            internal IReadOnlyCollection<PatientMedicalRecord> AddedRecords { get; private set; } =
                Array.Empty<PatientMedicalRecord>();

            public Task<IReadOnlyList<PatientMedicalRecord>> GetByIdsAsync(
                IReadOnlyCollection<PatientId> patientIds,
                CancellationToken cancellationToken = default)
            {
                GetCallCount++;
                return Task.FromResult(_existingRecords);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<PatientMedicalRecord> records,
                CancellationToken cancellationToken = default)
            {
                AddedRecords = records;
                return Task.CompletedTask;
            }
        }
    }
}

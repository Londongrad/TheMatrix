using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Care;
using Matrix.Healthcare.Application.Care.DeliverPatientCare;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Operations;
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
            var careActivityWriter = new CareDeliveryActivityOutboxWriterStub();
            var careNeedRepository = new CareNeedRepositoryStub();
            var batchSetRepository = new BatchSetRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                careNeedRepository,
                batchSetRepository,
                careDeliveryActivityOutboxWriter: careActivityWriter,
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
            Assert.Equal(0, outcome.FunctionalCapacityScore);
            Assert.Equal(0, outcome.LifecycleRevision);
            Assert.Equal(0, record.Health.Value);
            Assert.Equal(17, record.LastProgressionRevision);
            PatientHealthOutcomeBatch outboxBatch = Assert.Single(outboxWriter.Batches);
            Assert.Equal(17, outboxBatch.SourceRevision);
            Assert.Equal(CurrentDate, outboxBatch.CurrentDate);
            PatientCareNeed careNeed = Assert.Single(careNeedRepository.AddedCareNeeds);
            Assert.Equal(new PatientId(PatientGuid), careNeed.PatientId);
            Assert.Equal(CareNeedUrgency.Emergency, careNeed.Urgency);
            Assert.Equal(17, careNeed.LastAssessmentRevision);
            Assert.True(result.IsBatchSetComplete);
            Assert.True(result.CompletedBatchSetNow);
            Assert.Equal(1, batchSetRepository.AddCallCount);
            Assert.True(batchSetRepository.BatchSet!.HasReceivedBatch(1));
            CareDeliveryActivitySnapshot activity = Assert.Single(careActivityWriter.Activities);
            Assert.Equal(1, activity.ProcessedPatientCount);
            Assert.Equal(0, activity.RoutineCareDeliveryCount);
            Assert.Equal(0, activity.UrgentCareDeliveryCount);
            Assert.Equal(0, activity.AcuteCareDeliveryCount);
            Assert.Equal(0, activity.EmergencyCareDeliveryCount);
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
            Assert.True(result.IsBatchSetComplete);
            Assert.True(result.CompletedBatchSetNow);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(70, record.Health.Value);
        }

        [Fact]
        public async Task Handle_CompletedBatch_AllocatesCareAfterProgressionIsPersisted()
        {
            PatientMedicalRecord record = CreateMedicalRecord(health: 2);
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var careAllocator = new CareAllocatorStub(
                assignmentsCreated: 2,
                saveCount: () => unitOfWork.SaveCount);
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                new MedicalRecordRepositoryStub([record]),
                new OutcomeOutboxWriterStub(),
                careAllocator: careAllocator,
                unitOfWork: unitOfWork);

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17),
                CancellationToken.None);

            Assert.Equal(1, careAllocator.CallCount);
            Assert.Equal(1, careAllocator.SaveCountAtCall);
            Assert.Equal(CurrentDate.AddDays(1), careAllocator.CareDateAtCall);
            Assert.Equal(2, result.CareAssignmentsCreated);
            Assert.Equal(2, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DueAssignment_DeliversCareAndPublishesConsolidatedOutcome()
        {
            PatientMedicalRecord record = CreateMedicalRecord(health: 50);
            PatientCareNeed careNeed = PatientCareNeed.Register(
                new PatientId(PatientGuid),
                new SimulationHostId(HostId),
                CareNeedUrgency.Acute,
                requestedOn: CurrentDate.AddDays(-1),
                assessmentRevision: 16,
                lifecycleRevision: 0,
                assessedAtUtc: DateTimeOffset.Parse("2048-05-05T10:00:00+00:00"));
            var facilityId = new CareFacilityId(
                Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"));
            CareFacility facility = CareFacility.Register(
                facilityId,
                new SimulationHostId(HostId),
                "Central Hospital",
                new CareFacilityKindKey("Hospital"),
                locationAnchorId: null,
                dailyPatientCapacity: 20,
                isActive: true,
                sourceRevision: 7,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-05T10:00:00+00:00"));
            PatientCareAssignment assignment = PatientCareAssignment.Assign(
                PatientCareAssignmentId.New(),
                new SimulationHostId(HostId),
                new PatientId(PatientGuid),
                facilityId,
                CurrentDate,
                CareNeedUrgency.Acute,
                assessmentRevision: 16,
                lifecycleRevision: 0,
                assignedAtUtc: DateTimeOffset.Parse("2048-05-05T10:00:00+00:00"));
            var outboxWriter = new OutcomeOutboxWriterStub();
            var careActivityWriter = new CareDeliveryActivityOutboxWriterStub();
            var operationalProfileProvider = new CareOperationalProfileProviderStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                new MedicalRecordRepositoryStub([record]),
                outboxWriter,
                careNeedRepository: new CareNeedRepositoryStub([careNeed]),
                careAssignmentRepository: new CareAssignmentRepositoryStub([assignment]),
                careFacilityRepository: new CareFacilityRepositoryStub([facility]),
                careDeliveryActivityOutboxWriter: careActivityWriter,
                careOperationalProfileProvider: operationalProfileProvider);

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17),
                CancellationToken.None);

            PatientHealthProgressionResultItem outcome = Assert.Single(result.Outcomes);
            Assert.Equal(1, result.CareAssignmentsDelivered);
            Assert.Equal(0, result.CareAssignmentsCancelled);
            Assert.Equal(PatientCareAssignmentStatus.Delivered, assignment.Status);
            Assert.Equal(6, assignment.TreatmentHealthDelta);
            Assert.Equal(record.Health.Value - 50, outcome.HealthDelta);
            Assert.Equal(IllnessSeverity.Moderate, outcome.CurrentIllnessSeverity);
            Assert.Single(outboxWriter.Batches);
            CareDeliveryActivitySnapshot activity = Assert.Single(careActivityWriter.Activities);
            Assert.Equal(1, activity.ProcessedPatientCount);
            Assert.Equal(1, activity.AcuteCareDeliveryCount);
            Assert.Equal(1, operationalProfileProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WithoutDueAssignments_DoesNotLoadOperationalProfile()
        {
            var operationalProfileProvider = new CareOperationalProfileProviderStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                new MedicalRecordRepositoryStub([CreateMedicalRecord(health: 70)]),
                new OutcomeOutboxWriterStub(),
                careOperationalProfileProvider: operationalProfileProvider);

            await handler.Handle(
                CreateCommand(sourceRevision: 17),
                CancellationToken.None);

            Assert.Equal(0, operationalProfileProvider.CallCount);
        }

        [Fact]
        public async Task Handle_StaleLifecycleRisk_DoesNotOverrideLifecycleSnapshot()
        {
            PatientMedicalRecord record = CreateMedicalRecord(health: 100, lifecycleRevision: 1);
            var medicalRepository = new MedicalRecordRepositoryStub([record]);
            var outboxWriter = new OutcomeOutboxWriterStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                unitOfWork: unitOfWork,
                profile: CreateProfile(lifecycleRevision: 1));

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17, lifecycleRevision: 0),
                CancellationToken.None);

            Assert.Equal(0, result.ProcessedPatients);
            Assert.Equal(1, result.StalePatients);
            Assert.Equal(100, record.Health.Value);
            Assert.Empty(outboxWriter.Batches);
            Assert.True(result.IsBatchSetComplete);
            Assert.True(result.CompletedBatchSetNow);
            Assert.Equal(1, unitOfWork.SaveCount);
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

        [Fact]
        public async Task Handle_DuplicateBatch_SkipsPatientQueriesAndPersistence()
        {
            PatientHealthProgressionBatchSet batchSet = CreateBatchSet(
                totalBatches: 2,
                batchNumber: 1);
            var medicalRepository = new MedicalRecordRepositoryStub();
            var outboxWriter = new OutcomeOutboxWriterStub();
            var careNeedRepository = new CareNeedRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                careNeedRepository,
                new BatchSetRepositoryStub(batchSet),
                unitOfWork: unitOfWork);

            AdvancePatientHealthResult result = await handler.Handle(
                CreateCommand(sourceRevision: 17, batchNumber: 1, totalBatches: 2),
                CancellationToken.None);

            Assert.Equal(AdvancePatientHealthStatus.Applied, result.Status);
            Assert.Equal(0, result.ProcessedPatients);
            Assert.Equal(0, result.IgnoredPatients);
            Assert.Equal(0, result.StalePatients);
            Assert.False(result.IsBatchSetComplete);
            Assert.False(result.CompletedBatchSetNow);
            Assert.Equal(0, medicalRepository.GetCallCount);
            Assert.Equal(0, careNeedRepository.GetCallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_OutOfOrderLastBatch_CompletesExistingSet()
        {
            PatientHealthProgressionBatchSet batchSet = CreateBatchSet(
                totalBatches: 3,
                batchNumber: 2);
            var medicalRepository = new MedicalRecordRepositoryStub();
            var outboxWriter = new OutcomeOutboxWriterStub();
            var batchSetRepository = new BatchSetRepositoryStub(batchSet);
            var careAllocator = new CareAllocatorStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                batchSetRepository: batchSetRepository,
                careAllocator: careAllocator,
                unitOfWork: unitOfWork);

            AdvancePatientHealthResult first = await handler.Handle(
                CreateCommand(sourceRevision: 17, batchNumber: 3, totalBatches: 3),
                CancellationToken.None);
            AdvancePatientHealthResult completed = await handler.Handle(
                CreateCommand(sourceRevision: 17, batchNumber: 1, totalBatches: 3),
                CancellationToken.None);

            Assert.False(first.IsBatchSetComplete);
            Assert.False(first.CompletedBatchSetNow);
            Assert.True(completed.IsBatchSetComplete);
            Assert.True(completed.CompletedBatchSetNow);
            Assert.Equal(3, batchSet.ReceivedBatchCount);
            Assert.Equal(1, careAllocator.CallCount);
            Assert.Equal(2, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DuplicatePositionWithChangedMetadata_RejectsBeforePatientQueries()
        {
            PatientHealthProgressionBatchSet batchSet = CreateBatchSet(
                totalBatches: 2,
                batchNumber: 1);
            var medicalRepository = new MedicalRecordRepositoryStub();
            var outboxWriter = new OutcomeOutboxWriterStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                medicalRepository,
                outboxWriter,
                batchSetRepository: new BatchSetRepositoryStub(batchSet),
                unitOfWork: unitOfWork);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                CreateCommand(
                    sourceRevision: 17,
                    batchNumber: 1,
                    totalBatches: 2,
                    correlationId: "changed"),
                CancellationToken.None));

            Assert.Equal(0, medicalRepository.GetCallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_ExcessiveBatchSet_DoesNotOpenTransaction()
        {
            var unitOfWork = new HealthcareUnitOfWorkStub();
            AdvancePatientHealthCommandHandler handler = CreateHandler(
                new MedicalRecordRepositoryStub(),
                new OutcomeOutboxWriterStub(),
                unitOfWork: unitOfWork);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(
                CreateCommand(
                    sourceRevision: 17,
                    totalBatches: PatientHealthProgressionBatchSet.MaxTotalBatches + 1),
                CancellationToken.None));

            Assert.Equal(0, unitOfWork.TransactionCount);
        }

        private static AdvancePatientHealthCommandHandler CreateHandler(
            MedicalRecordRepositoryStub medicalRepository,
            OutcomeOutboxWriterStub outboxWriter,
            CareNeedRepositoryStub? careNeedRepository = null,
            BatchSetRepositoryStub? batchSetRepository = null,
            CareAllocatorStub? careAllocator = null,
            CareAssignmentRepositoryStub? careAssignmentRepository = null,
            CareFacilityRepositoryStub? careFacilityRepository = null,
            CareDeliveryActivityOutboxWriterStub? careDeliveryActivityOutboxWriter = null,
            CareOperationalProfileProviderStub? careOperationalProfileProvider = null,
            DateTimeOffset? deletedAtUtc = null,
            HealthcareUnitOfWorkStub? unitOfWork = null,
            PatientProfile? profile = null)
        {
            return new AdvancePatientHealthCommandHandler(
                patientProfileRepository: new PatientProfileRepositoryStub([profile ?? CreateProfile()]),
                medicalRecordRepository: medicalRepository,
                patientCareNeedRepository: careNeedRepository ?? new CareNeedRepositoryStub(),
                patientCareAssignmentRepository: careAssignmentRepository
                                                 ?? new CareAssignmentRepositoryStub(),
                careFacilityRepository: careFacilityRepository
                                        ?? new CareFacilityRepositoryStub(),
                batchSetRepository: batchSetRepository ?? new BatchSetRepositoryStub(),
                careAllocator: careAllocator ?? new CareAllocatorStub(),
                deletionRepository: new HealthcareSimulationDeletionRepositoryStub(deletedAtUtc),
                outcomeOutboxWriter: outboxWriter,
                careDeliveryActivityOutboxWriter: careDeliveryActivityOutboxWriter
                                                  ?? new CareDeliveryActivityOutboxWriterStub(),
                progressionPolicy: CreatePolicy(),
                functionalCapacityPolicy: new PatientFunctionalCapacityPolicy(),
                careNeedAssessmentPolicy: new PatientCareNeedAssessmentPolicy(),
                careDeliveryService: new PatientCareDeliveryService(
                    new PatientCareTreatmentPolicy()),
                careOperationalProfileProvider: careOperationalProfileProvider
                                                ?? new CareOperationalProfileProviderStub(),
                unitOfWork: unitOfWork ?? new HealthcareUnitOfWorkStub());
        }

        private static AdvancePatientHealthCommand CreateCommand(
            long sourceRevision,
            long lifecycleRevision = 0,
            int batchNumber = 1,
            int totalBatches = 1,
            string? correlationId = null)
        {
            return new AdvancePatientHealthCommand(
                SimulationHostId: HostId,
                SourceRevision: sourceRevision,
                PreviousDate: CurrentDate.AddDays(-1),
                CurrentDate: CurrentDate,
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: correlationId ?? $"health-risk:{sourceRevision}",
                BatchNumber: batchNumber,
                TotalBatches: totalBatches,
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
                        PublicHealthRiskStrength: 1d,
                        LifecycleRevision: lifecycleRevision)
                ]);
        }

        private static PatientHealthProgressionBatchSet CreateBatchSet(
            int totalBatches,
            int batchNumber)
        {
            PatientHealthProgressionBatchSet batchSet = PatientHealthProgressionBatchSet.Start(
                simulationHostId: new SimulationHostId(HostId),
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: totalBatches,
                batchNumber: batchNumber,
                currentDate: CurrentDate,
                receivedAtUtc: DateTimeOffset.Parse("2048-05-06T09:59:00+00:00"));
            batchSet.RecordCareDeliveryBatch(
                processedPatientCount: 0,
                routineCareDeliveryCount: 0,
                urgentCareDeliveryCount: 0,
                acuteCareDeliveryCount: 0,
                emergencyCareDeliveryCount: 0);
            return batchSet;
        }

        private static PatientProfile CreateProfile(long lifecycleRevision = 0)
        {
            return PatientProfile.Register(
                new PatientId(PatientGuid),
                new SimulationHostId(HostId),
                birthDate: new DateOnly(2030, 5, 6),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: 16,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T09:59:00+00:00"),
                lifecycleRevision: lifecycleRevision);
        }

        private static PatientMedicalRecord CreateMedicalRecord(
            int health,
            long lifecycleRevision = 0)
        {
            return PatientMedicalRecord.Register(
                new PatientId(PatientGuid),
                new SimulationHostId(HostId),
                new HealthScore(health),
                PatientIllnessState.Active(
                    IllnessKind.Infection,
                    IllnessSeverity.Severe,
                    CurrentDate.AddDays(-3)),
                lifecycleRevision: lifecycleRevision);
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

            public Task<PatientPopulationHealthBurden> GetPopulationHealthBurdenAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(PatientPopulationHealthBurden.Empty);
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

        private sealed class CareDeliveryActivityOutboxWriterStub : ICareDeliveryActivityOutboxWriter
        {
            internal List<CareDeliveryActivitySnapshot> Activities { get; } = [];

            public Task AddAsync(
                CareDeliveryActivitySnapshot activity,
                CancellationToken cancellationToken = default)
            {
                Activities.Add(activity);
                return Task.CompletedTask;
            }
        }

        private sealed class CareNeedRepositoryStub(
            IReadOnlyList<PatientCareNeed>? careNeeds = null) : IPatientCareNeedRepository
        {
            private readonly IReadOnlyList<PatientCareNeed> _careNeeds =
                careNeeds ?? Array.Empty<PatientCareNeed>();

            internal List<PatientCareNeed> AddedCareNeeds { get; } = [];
            internal int GetCallCount { get; private set; }

            public Task<IReadOnlyList<PatientCareNeed>> GetByPatientIdsAsync(
                IReadOnlyCollection<PatientId> patientIds,
                CancellationToken cancellationToken = default)
            {
                GetCallCount++;
                return Task.FromResult(_careNeeds);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<PatientCareNeed> careNeedsToAdd,
                CancellationToken cancellationToken = default)
            {
                AddedCareNeeds.AddRange(careNeedsToAdd);
                return Task.CompletedTask;
            }
        }

        private sealed class BatchSetRepositoryStub(
            PatientHealthProgressionBatchSet? batchSet = null)
            : IPatientHealthProgressionBatchSetRepository
        {
            internal PatientHealthProgressionBatchSet? BatchSet { get; private set; } = batchSet;
            internal int GetCallCount { get; private set; }
            internal int AddCallCount { get; private set; }

            public Task<PatientHealthProgressionBatchSet?> GetAsync(
                SimulationHostId simulationHostId,
                long sourceRevision,
                CancellationToken cancellationToken = default)
            {
                GetCallCount++;
                return Task.FromResult(
                    BatchSet is not null
                    && BatchSet.SimulationHostId == simulationHostId
                    && BatchSet.SourceRevision == sourceRevision
                        ? BatchSet
                        : null);
            }

            public Task AddAsync(
                PatientHealthProgressionBatchSet batchSetToAdd,
                CancellationToken cancellationToken = default)
            {
                AddCallCount++;
                BatchSet = batchSetToAdd;
                return Task.CompletedTask;
            }
        }

        private sealed class CareAssignmentRepositoryStub(
            IReadOnlyList<PatientCareAssignment>? assignments = null)
            : IPatientCareAssignmentRepository
        {
            private readonly IReadOnlyList<PatientCareAssignment> _assignments =
                assignments ?? Array.Empty<PatientCareAssignment>();

            public Task<IReadOnlyList<PatientCareAssignment>> GetDueScheduledByPatientIdsAsync(
                SimulationHostId simulationHostId,
                IReadOnlyCollection<PatientId> patientIds,
                DateOnly dueThroughDate,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_assignments);
            }
        }

        private sealed class CareFacilityRepositoryStub(
            IReadOnlyList<CareFacility>? facilities = null) : ICareFacilityRepository
        {
            private readonly IReadOnlyList<CareFacility> _facilities =
                facilities ?? Array.Empty<CareFacility>();

            public Task<IReadOnlyList<CareFacility>> GetByIdsAsync(
                IReadOnlyCollection<CareFacilityId> facilityIds,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_facilities);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<CareFacility> facilities,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class CareOperationalProfileProviderStub(
            CareOperationalProfile? profile = null) : ICareOperationalProfileProvider
        {
            internal int CallCount { get; private set; }

            public Task<CareOperationalProfile> GetAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(profile ?? CareOperationalProfile.Baseline);
            }
        }

        private sealed class CareAllocatorStub(
            int assignmentsCreated = 0,
            Func<int>? saveCount = null) : IPatientCareAllocator
        {
            internal int CallCount { get; private set; }
            internal int? SaveCountAtCall { get; private set; }
            internal DateOnly? CareDateAtCall { get; private set; }

            public Task<int> AllocateAsync(
                SimulationHostId simulationHostId,
                DateOnly careDate,
                DateTimeOffset assignedAtUtc,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                SaveCountAtCall = saveCount?.Invoke();
                CareDateAtCall = careDate;
                return Task.FromResult(assignmentsCreated);
            }
        }
    }
}

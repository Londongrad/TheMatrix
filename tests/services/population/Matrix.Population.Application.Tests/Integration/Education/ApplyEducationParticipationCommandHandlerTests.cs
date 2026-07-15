using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Integration.Education.ApplyEducationParticipation;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Xunit;

namespace Matrix.Population.Application.Tests.Integration.Education
{
    public sealed class ApplyEducationParticipationCommandHandlerTests
    {
        private static readonly Guid HostId =
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ResidentId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public async Task Handle_CurrentResident_StoresExternalProjection()
        {
            Person resident = PopulationApplicationTestSupport.CreatePerson(ResidentId);
            var personRepository = new PopulationApplicationTestSupport.FakePersonReadRepository();
            personRepository.PersonsById.Add(resident.Id, resident);
            var projectionRepository =
                new PopulationApplicationTestSupport.FakeEducationParticipationProjectionRepository();
            var unitOfWork = new PopulationApplicationTestSupport.FakeUnitOfWork();
            var handler = CreateHandler(personRepository, projectionRepository, unitOfWork: unitOfWork);

            ApplyEducationParticipationResult result = await handler.Handle(
                CreateCommand(CreateStudent()),
                CancellationToken.None);

            Assert.Equal(ApplyEducationParticipationStatus.Applied, result.Status);
            Assert.Equal(1, result.AppliedStudentCount);
            Assert.Equal(0, result.StaleStudentCount);
            Assert.Equal(0, result.MissingOrChangedResidentCount);
            EducationParticipationProjection projection =
                Assert.Single(projectionRepository.Projections);
            Assert.Equal(HostId, projection.SimulationHostId);
            Assert.Equal("primary", projection.ActiveStage);
            Assert.Equal(PopulationApplicationTestSupport.UtcNow, projection.UpdatedAtUtc);
            Assert.Equal(1, unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_ChangedResidentLifecycle_IgnoresStaleParticipation()
        {
            Person resident = PopulationApplicationTestSupport.CreatePerson(ResidentId);
            var personRepository = new PopulationApplicationTestSupport.FakePersonReadRepository();
            personRepository.PersonsById.Add(resident.Id, resident);
            var projectionRepository =
                new PopulationApplicationTestSupport.FakeEducationParticipationProjectionRepository();
            var handler = CreateHandler(personRepository, projectionRepository);
            StudentEducationParticipationInput stale = CreateStudent() with
            {
                ResidentLifecycleRevision = 1
            };

            ApplyEducationParticipationResult result = await handler.Handle(
                CreateCommand(stale),
                CancellationToken.None);

            Assert.Equal(0, result.AppliedStudentCount);
            Assert.Equal(1, result.MissingOrChangedResidentCount);
            Assert.Empty(projectionRepository.Projections);
            Assert.Equal(0, projectionRepository.UpsertCallCount);
        }

        [Fact]
        public async Task Handle_OlderParticipationRevision_ReportsStaleProjection()
        {
            Person resident = PopulationApplicationTestSupport.CreatePerson(ResidentId);
            var personRepository = new PopulationApplicationTestSupport.FakePersonReadRepository();
            personRepository.PersonsById.Add(resident.Id, resident);
            var projectionRepository =
                new PopulationApplicationTestSupport.FakeEducationParticipationProjectionRepository();
            await projectionRepository.UpsertNewerAsync(
            [
                new EducationParticipationProjection(
                    SimulationHostId: HostId,
                    ResidentId: ResidentId,
                    ParticipationRevision: 2,
                    ResidentLifecycleRevision: 0,
                    IsEnrolled: false,
                    ActiveStage: null,
                    InstitutionId: null,
                    InstitutionAnchorId: null,
                    EnrolledOn: null,
                    CompletedStage: "primary",
                    CompletedStageOn: new DateOnly(2048, 1, 1),
                    SnapshotDate: new DateOnly(2048, 1, 1),
                    OccurredAtUtc: PopulationApplicationTestSupport.UtcNow,
                    UpdatedAtUtc: PopulationApplicationTestSupport.UtcNow)
            ]);
            var handler = CreateHandler(personRepository, projectionRepository);

            ApplyEducationParticipationResult result = await handler.Handle(
                CreateCommand(CreateStudent()),
                CancellationToken.None);

            Assert.Equal(0, result.AppliedStudentCount);
            Assert.Equal(1, result.StaleStudentCount);
            Assert.Equal(2, Assert.Single(projectionRepository.Projections).ParticipationRevision);
        }

        [Fact]
        public async Task Handle_DuplicateMessage_SkipsProjectionWork()
        {
            var processedRepository =
                new PopulationApplicationTestSupport.FakeProcessedIntegrationMessageRepository
                {
                    TryMarkProcessedResult = false
                };
            var projectionRepository =
                new PopulationApplicationTestSupport.FakeEducationParticipationProjectionRepository();
            var unitOfWork = new PopulationApplicationTestSupport.FakeUnitOfWork();
            var handler = CreateHandler(
                new PopulationApplicationTestSupport.FakePersonReadRepository(),
                projectionRepository,
                processedRepository,
                unitOfWork);

            ApplyEducationParticipationResult result = await handler.Handle(
                CreateCommand(CreateStudent()),
                CancellationToken.None);

            Assert.Equal(ApplyEducationParticipationStatus.Duplicate, result.Status);
            Assert.Equal(0, projectionRepository.UpsertCallCount);
            Assert.Equal(0, unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_InconsistentEnrollment_DoesNotOpenTransaction()
        {
            var unitOfWork = new PopulationApplicationTestSupport.FakeUnitOfWork();
            var handler = CreateHandler(
                new PopulationApplicationTestSupport.FakePersonReadRepository(),
                new PopulationApplicationTestSupport.FakeEducationParticipationProjectionRepository(),
                unitOfWork: unitOfWork);
            StudentEducationParticipationInput invalid = CreateStudent() with
            {
                InstitutionId = null
            };

            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
                CreateCommand(invalid),
                CancellationToken.None));

            Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
        }

        private static ApplyEducationParticipationCommandHandler CreateHandler(
            PopulationApplicationTestSupport.FakePersonReadRepository personRepository,
            PopulationApplicationTestSupport.FakeEducationParticipationProjectionRepository projectionRepository,
            PopulationApplicationTestSupport.FakeProcessedIntegrationMessageRepository? processedRepository = null,
            PopulationApplicationTestSupport.FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyEducationParticipationCommandHandler(
                personRepository,
                projectionRepository,
                processedRepository ??
                new PopulationApplicationTestSupport.FakeProcessedIntegrationMessageRepository(),
                unitOfWork ?? new PopulationApplicationTestSupport.FakeUnitOfWork(),
                PopulationApplicationTestSupport.CreateTimeProvider());
        }

        private static ApplyEducationParticipationCommand CreateCommand(
            StudentEducationParticipationInput student)
        {
            return new ApplyEducationParticipationCommand(
                SimulationHostId: HostId,
                IntegrationMessageId: Guid.NewGuid(),
                ConsumerName: "population-education-participation-v1",
                SnapshotDate: new DateOnly(2048, 5, 3),
                OccurredAtUtc: PopulationApplicationTestSupport.UtcNow,
                BatchNumber: 1,
                TotalBatches: 1,
                Students: [student]);
        }

        private static StudentEducationParticipationInput CreateStudent()
        {
            return new StudentEducationParticipationInput(
                ResidentId: ResidentId,
                ParticipationRevision: 1,
                ResidentLifecycleRevision: 0,
                IsEnrolled: true,
                ActiveStage: "primary",
                InstitutionId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                InstitutionAnchorId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                EnrolledOn: new DateOnly(2048, 5, 1),
                CompletedStage: "preschool",
                CompletedStageOn: new DateOnly(2047, 6, 30));
        }
    }
}

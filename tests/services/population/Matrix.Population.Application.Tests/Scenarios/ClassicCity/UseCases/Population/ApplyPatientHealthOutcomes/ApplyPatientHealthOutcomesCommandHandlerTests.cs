using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Services;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes
{
    public sealed class ApplyPatientHealthOutcomesCommandHandlerTests
    {
        private static readonly Guid CityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateOnly CurrentDate = new(2048, 5, 6);

        [Fact]
        public async Task Handle_LethalOutcome_AppliesProjectionWidowhoodAndLifecycleFact()
        {
            var marriageDomainService = new MarriageDomainService();
            Guid householdId = Guid.NewGuid();
            Person patient = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                householdId: householdId,
                sex: Sex.Male,
                currentDate: CurrentDate);
            Person spouse = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: householdId,
                sex: Sex.Female,
                firstName: "Trinity",
                currentDate: CurrentDate);
            marriageDomainService.RegisterMarriage(patient, spouse, CurrentDate);
            var personRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [patient, spouse]
            };
            var factsWriter = new FakePopulationResidentFactsOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            ApplyPatientHealthOutcomesCommandHandler handler = CreateHandler(
                personRepository,
                factsWriter,
                marriageDomainService,
                unitOfWork: unitOfWork);

            ApplyPatientHealthOutcomesResult result = await handler.Handle(
                CreateCommand(
                    new PatientHealthOutcomeInput(
                        PatientId: patient.Id.Value,
                        HealthScore: 0,
                        HappinessDelta: -3,
                        EnergyDelta: -2,
                        StressDelta: 2)),
                CancellationToken.None);

            Assert.Equal(ApplyPatientHealthOutcomesStatus.Applied, result.Status);
            Assert.Equal(1, result.AppliedPatientCount);
            Assert.False(patient.IsAlive);
            Assert.Equal(MaritalStatus.Widowed, spouse.MaritalStatus);
            Assert.Null(spouse.SpouseId);
            Assert.Equal(17, patient.LastHealthcareRevision);
            Assert.Equal(1, unitOfWork.SaveChangesCalls);
            Assert.False(Assert.Single(Assert.Single(factsWriter.Batches).Residents).IsAlive);
        }

        [Fact]
        public async Task Handle_DuplicateMessage_DoesNotLoadOrSaveResidents()
        {
            var processedRepository = new FakeProcessedIntegrationMessageRepository
            {
                TryMarkProcessedResult = false
            };
            var personRepository = new FakeCityPopulationPersonReadRepository();
            var unitOfWork = new FakeUnitOfWork();
            ApplyPatientHealthOutcomesCommandHandler handler = CreateHandler(
                personRepository,
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ApplyPatientHealthOutcomesResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(ApplyPatientHealthOutcomesStatus.Duplicate, result.Status);
            Assert.Null(personRepository.RequestedCityId);
            Assert.Equal(0, unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_StalePatientRevision_DoesNotOverwriteProjection()
        {
            Person patient = CreatePerson(currentDate: CurrentDate);
            patient.TryApplyHealthcareOutcome(
                sourceRevision: 18,
                healthScore: 64,
                happinessDelta: 0,
                energyDelta: 0,
                stressDelta: 0,
                currentDate: CurrentDate);
            var personRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [patient]
            };
            ApplyPatientHealthOutcomesCommandHandler handler = CreateHandler(personRepository);

            ApplyPatientHealthOutcomesResult result = await handler.Handle(
                CreateCommand(
                    new PatientHealthOutcomeInput(
                        patient.Id.Value,
                        10,
                        -10,
                        -10,
                        10)),
                CancellationToken.None);

            Assert.Equal(0, result.AppliedPatientCount);
            Assert.Equal(1, result.StalePatientCount);
            Assert.Equal(64, patient.Health.Value);
        }

        [Fact]
        public async Task Handle_OutcomeFromPreviousLifecycle_DoesNotOverrideResurrection()
        {
            Person patient = CreatePerson(
                lifeStatus: LifeStatus.Deceased,
                currentDate: CurrentDate);
            long deceasedRevision = patient.LifecycleRevision;
            patient.Resurrect();
            var personRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [patient]
            };
            ApplyPatientHealthOutcomesCommandHandler handler = CreateHandler(personRepository);

            ApplyPatientHealthOutcomesResult result = await handler.Handle(
                CreateCommand(
                    new PatientHealthOutcomeInput(
                        patient.Id.Value,
                        0,
                        0,
                        0,
                        0,
                        LifecycleRevision: deceasedRevision)),
                CancellationToken.None);

            Assert.Equal(0, result.AppliedPatientCount);
            Assert.Equal(1, result.StalePatientCount);
            Assert.True(patient.IsAlive);
            Assert.Equal(100, patient.Health.Value);
        }

        private static ApplyPatientHealthOutcomesCommandHandler CreateHandler(
            FakeCityPopulationPersonReadRepository? personRepository = null,
            FakePopulationResidentFactsOutboxWriter? factsWriter = null,
            MarriageDomainService? marriageDomainService = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyPatientHealthOutcomesCommandHandler(
                personReadRepository: personRepository ?? new FakeCityPopulationPersonReadRepository(),
                archiveStateRepository: new FakeCityPopulationArchiveStateRepository(),
                deletionStateRepository: new FakeCityPopulationDeletionStateRepository(),
                processedMessageRepository: processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
                residentFactsOutboxWriter: factsWriter ?? new FakePopulationResidentFactsOutboxWriter(),
                marriageDomainService: marriageDomainService ?? new MarriageDomainService(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ApplyPatientHealthOutcomesCommand CreateCommand(
            params PatientHealthOutcomeInput[] patients)
        {
            return new ApplyPatientHealthOutcomesCommand(
                CityId: CityId,
                IntegrationMessageId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConsumerName: "population-healthcare-patient-health-outcome-v1",
                SourceRevision: 17,
                CurrentDate: CurrentDate,
                OccurredAtUtc: UtcNow,
                CorrelationId: "healthcare:city:17:outcome",
                BatchNumber: 1,
                TotalBatches: 1,
                Patients: patients);
        }
    }
}

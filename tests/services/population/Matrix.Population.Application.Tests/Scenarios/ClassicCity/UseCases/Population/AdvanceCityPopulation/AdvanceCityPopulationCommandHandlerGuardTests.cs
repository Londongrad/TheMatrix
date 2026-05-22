using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class AdvanceCityPopulationCommandHandlerGuardTests
{
    [Fact]
    public async Task Handle_WhenTickIsAlreadyProcessed_ReturnsDuplicate()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
        {
            State = CityPopulationProgressionState.Create(
                cityId: CityId.From(cityId),
                lastProcessedTickId: 10,
                lastProcessedDate: new DateOnly(2048, 5, 5),
                updatedAtUtc: UtcNow)
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            progressionStateRepository: progressionStateRepository,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            CreateCommand(cityId: cityId, tickId: 10),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.Duplicate, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Equal(CityId.From(cityId), progressionStateRepository.RequestedCityId);
        Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDateMovesBackwards_ReturnsOutOfOrder()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
        {
            State = CityPopulationProgressionState.Create(
                cityId: CityId.From(cityId),
                lastProcessedTickId: 8,
                lastProcessedDate: new DateOnly(2048, 5, 6),
                updatedAtUtc: UtcNow)
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            progressionStateRepository: progressionStateRepository,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            new AdvanceCityPopulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(2048, 5, 5, 9, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 5, 10, 0, 0, TimeSpan.Zero),
                TickId: 9),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.OutOfOrder, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCityIsDeleted_ReturnsCityDeleted()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: UtcNow,
                updatedAtUtc: UtcNow)
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            CreateCommand(cityId: cityId, tickId: 11),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.CityDeleted, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Equal(CityId.From(cityId), deletionStateRepository.RequestedCityId);
        Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCityIsArchived_ReturnsCityArchived()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
        {
            State = CityPopulationArchiveState.Create(
                cityId: CityId.From(cityId),
                archivedAtUtc: UtcNow,
                updatedAtUtc: UtcNow)
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            CreateCommand(cityId: cityId, tickId: 11),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.CityArchived, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Equal(CityId.From(cityId), archiveStateRepository.RequestedCityId);
        Assert.Equal(0, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static AdvanceCityPopulationCommand CreateCommand(Guid cityId, long tickId)
    {
        return new AdvanceCityPopulationCommand(
            CityId: cityId,
            FromSimTimeUtc: new DateTimeOffset(2048, 5, 5, 9, 0, 0, TimeSpan.Zero),
            ToSimTimeUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
            TickId: tickId);
    }

    private static AdvanceCityPopulationCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        var householdLivelihoodPolicy = new CityHouseholdLivelihoodPolicy();
        var householdCashflowPolicy = new CityHouseholdCashflowPolicy();
        var householdEconomyPolicy = new CityHouseholdEconomyPolicy(
            householdLivelihoodPolicy,
            householdCashflowPolicy);
        var anchorSelectionPolicy = new CityPopulationAnchorSelectionPolicy();

        return new AdvanceCityPopulationCommandHandler(
            personReadRepository: new FakeCityPopulationPersonReadRepository(),
            cityPopulationArchiveStateRepository: archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            cityPopulationAnchorCatalogRepository: new FakeCityPopulationAnchorCatalogRepository(),
            cityPopulationCostOfLivingStateRepository: new FakeCityPopulationCostOfLivingStateRepository(),
            cityPopulationEssentialsStateRepository: new FakeCityPopulationEssentialsStateRepository(),
            cityPopulationServiceQualityStateRepository: new FakeCityPopulationServiceQualityStateRepository(),
            cityPopulationDeletionStateRepository: deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            employerFinancialStressStateRepository: new FakeCityPopulationEmployerFinancialStressStateRepository(),
            cityPopulationEnvironmentRepository: new FakeCityPopulationEnvironmentRepository(),
            householdFinancialStressStateRepository: new FakeCityPopulationHouseholdFinancialStressStateRepository(),
            cityPopulationLivingConditionsStateRepository: new FakeCityPopulationLivingConditionsStateRepository(),
            districtUtilityConditionsClient: new FakeCityDistrictUtilityConditionsClient(),
            commuteRoutingService: new FakeCityPopulationCommuteRoutingService(),
            commuteTripSyncService: new FakeCityPopulationCommuteTripSyncService(),
            cityPopulationActivityJournalService: new FakeCityPopulationActivityJournalService(),
            cityEconomySettlementOutboxWriter: new FakeCityEconomySettlementOutboxWriter(),
            progressionStateRepository: progressionStateRepository ?? new FakeCityPopulationProgressionStateRepository(),
            cityPopulationSummaryProjectionService: new FakeCityPopulationSummaryProjectionService(),
            weatherExposureStateRepository: new FakeCityPopulationWeatherExposureStateRepository(),
            householdWriteRepository: new FakeHouseholdWriteRepository(),
            marriageDomainService: new MarriageDomainService(),
            populationBirthDomainService: new PopulationBirthDomainService(),
            personWriteRepository: new FakePersonWriteRepository(),
            birthAutonomyPolicy: new CityBirthAutonomyPolicy(new TestPopulationGenerationContentCatalog(), householdLivelihoodPolicy),
            civilRegistryAutonomyPolicy: new CityCivilRegistryAutonomyPolicy(),
            educationAutonomyPolicy: new CityEducationAutonomyPolicy(anchorSelectionPolicy),
            employmentAutonomyPolicy: new CityEmploymentAutonomyPolicy(new TestPopulationGenerationContentCatalog(), householdEconomyPolicy, anchorSelectionPolicy),
            healthcareAutonomyPolicy: new CityHealthcareAutonomyPolicy(householdLivelihoodPolicy),
            householdCashflowPolicy: householdCashflowPolicy,
            householdPressurePolicy: new CityHouseholdPressurePolicy(),
            housingAutonomyPolicy: new CityHousingAutonomyPolicy(householdEconomyPolicy),
            householdIndependenceAutonomyPolicy: new CityHouseholdIndependenceAutonomyPolicy(householdLivelihoodPolicy),
            anchorSelectionPolicy: anchorSelectionPolicy,
            districtImpactPolicy: new CityPopulationDistrictImpactPolicy(),
            healthcarePressurePolicy: new CityPopulationHealthcarePressurePolicy(),
            illnessAutonomyPolicy: new CityIllnessAutonomyPolicy(),
            livingConditionsPressurePolicy: new CityPopulationLivingConditionsPressurePolicy(),
            participationPolicy: new CityPopulationParticipationPolicy(),
            personNeedsProgressionPolicy: new PersonNeedsProgressionPolicy(),
            weatherExposurePolicy: new CityPopulationWeatherExposurePolicy(new CityPopulationClimateAdaptationPolicy()),
            timeProvider: CreateTimeProvider(),
            logger: NullLogger<AdvanceCityPopulationCommandHandler>.Instance,
            unitOfWork: unitOfWork ?? new FakeUnitOfWork());
    }

    private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
    {
        public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
        public IReadOnlyList<string> FemaleFirstNames => ["Anna"];
        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => [new("Ivanov", "Ivanova")];
        public IReadOnlyList<PopulationProfessionCatalogItem> Professions => [new("Engineer", 1)];
    }
}

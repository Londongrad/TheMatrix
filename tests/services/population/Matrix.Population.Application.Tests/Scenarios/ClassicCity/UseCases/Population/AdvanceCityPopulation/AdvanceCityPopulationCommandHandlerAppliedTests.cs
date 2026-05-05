using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class AdvanceCityPopulationCommandHandlerAppliedTests
{
    [Fact]
    public async Task Handle_WhenCityHasNoResidents_CreatesProgressionStateAndReturnsApplied()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Person>()
        };
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            HouseholdsByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Household>(),
            PlacementsByCityResult = Array.Empty<Matrix.Population.Domain.Scenarios.ClassicCity.Entities.ClassicCityHouseholdPlacement>()
        };
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
        var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            progressionStateRepository: progressionStateRepository,
            householdWriteRepository: householdWriteRepository,
            summaryProjectionService: summaryProjectionService,
            activityJournalService: activityJournalService,
            outboxWriter: outboxWriter,
            commuteTripSyncService: commuteTripSyncService,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            CreateCommand(cityId: cityId, tickId: 12),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.Applied, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Single(progressionStateRepository.AddedStates);
        Assert.NotNull(progressionStateRepository.State);
        Assert.Equal(12, progressionStateRepository.State!.LastProcessedTickId);
        Assert.Equal(new DateOnly(2048, 5, 6), progressionStateRepository.State.LastProcessedDate);
        Assert.Equal(CityId.From(cityId), personReadRepository.RequestedCityId);
        Assert.Equal(CityId.From(cityId), householdWriteRepository.RequestedCityId);
        Assert.Single(summaryProjectionService.UpdateCalls);
        Assert.Equal((CityId.From(cityId), new DateOnly(2048, 5, 6), 0, 0, true), summaryProjectionService.UpdateCalls[0]);
        Assert.Empty(activityJournalService.Entries);
        Assert.Empty(outboxWriter.HouseholdBatches);
        Assert.Empty(outboxWriter.WorkplaceBatches);
        Assert.Equal(1, commuteTripSyncService.SyncCalls);
        Assert.Equal(1, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenSameDayTickAdvancesOnlyState_MarksProgressWithoutResidentWork()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository();
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
        {
            State = CityPopulationProgressionState.Create(
                cityId: CityId.From(cityId),
                lastProcessedTickId: 12,
                lastProcessedDate: new DateOnly(2048, 5, 6),
                updatedAtUtc: UtcNow)
        };
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
        var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            progressionStateRepository: progressionStateRepository,
            householdWriteRepository: householdWriteRepository,
            summaryProjectionService: summaryProjectionService,
            activityJournalService: activityJournalService,
            outboxWriter: outboxWriter,
            commuteTripSyncService: commuteTripSyncService,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            new AdvanceCityPopulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
                TickId: 13),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.Applied, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Empty(progressionStateRepository.AddedStates);
        Assert.NotNull(progressionStateRepository.State);
        Assert.Equal(13, progressionStateRepository.State!.LastProcessedTickId);
        Assert.Equal(new DateOnly(2048, 5, 6), progressionStateRepository.State.LastProcessedDate);
        Assert.Null(personReadRepository.RequestedCityId);
        Assert.Null(householdWriteRepository.RequestedCityId);
        Assert.Empty(summaryProjectionService.UpdateCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Empty(outboxWriter.HouseholdBatches);
        Assert.Empty(outboxWriter.WorkplaceBatches);
        Assert.Equal(0, commuteTripSyncService.SyncCalls);
        Assert.Equal(1, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenWeatherExposureCheckpointAdvances_MarksExposureProcessed()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Person>()
        };
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
        {
            State = CityPopulationProgressionState.Create(
                cityId: CityId.From(cityId),
                lastProcessedTickId: 13,
                lastProcessedDate: new DateOnly(2048, 5, 6),
                updatedAtUtc: UtcNow)
        };
        DateTimeOffset currentWeatherAt = new(2048, 5, 6, 8, 0, 0, TimeSpan.Zero);
        var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository
        {
            State = CityPopulationWeatherExposureState.Create(
                cityId: CityId.From(cityId),
                currentWeather: new WeatherImpactProfile(
                    Type: PopulationWeatherType.Clear,
                    Severity: PopulationWeatherSeverity.Calm,
                    PrecipitationKind: PopulationPrecipitationKind.None,
                    TemperatureC: 14m,
                    HumidityPercent: 52m,
                    WindSpeedKph: 8m,
                    CloudCoveragePercent: 10m,
                    PressureHpa: 1009m),
                currentWeatherEffectiveAtSimTimeUtc: currentWeatherAt,
                occurredOnUtc: currentWeatherAt,
                updatedAtUtc: currentWeatherAt)
        };
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            HouseholdsByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Household>(),
            PlacementsByCityResult = Array.Empty<ClassicCityHouseholdPlacement>()
        };
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            progressionStateRepository: progressionStateRepository,
            weatherExposureStateRepository: weatherExposureStateRepository,
            householdWriteRepository: householdWriteRepository,
            summaryProjectionService: summaryProjectionService,
            commuteTripSyncService: commuteTripSyncService,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            new AdvanceCityPopulationCommand(
                CityId: cityId,
                FromSimTimeUtc: currentWeatherAt,
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero),
                TickId: 14),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.Applied, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.NotNull(weatherExposureStateRepository.State);
        Assert.Equal(new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero), weatherExposureStateRepository.State!.LastExposureProcessedAtSimTimeUtc);
        Assert.Equal(14, progressionStateRepository.State!.LastProcessedTickId);
        Assert.Equal(CityId.From(cityId), personReadRepository.RequestedCityId);
        Assert.Single(summaryProjectionService.UpdateCalls);
        Assert.Equal(1, commuteTripSyncService.SyncCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCommuteTripSyncFails_ReturnsApplied()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Person>()
        };
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            HouseholdsByCityResult = Array.Empty<Matrix.Population.Domain.Entities.Household>(),
            PlacementsByCityResult = Array.Empty<ClassicCityHouseholdPlacement>()
        };
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService
        {
            ExceptionToThrow = new InvalidOperationException("sync failed")
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            progressionStateRepository: progressionStateRepository,
            householdWriteRepository: householdWriteRepository,
            summaryProjectionService: summaryProjectionService,
            commuteTripSyncService: commuteTripSyncService,
            unitOfWork: unitOfWork);

        AdvanceCityPopulationResult result = await handler.Handle(
            CreateCommand(cityId: cityId, tickId: 15),
            CancellationToken.None);

        Assert.Equal(AdvanceCityPopulationStatus.Applied, result.Status);
        Assert.Equal(0, result.AffectedPeopleCount);
        Assert.Single(progressionStateRepository.AddedStates);
        Assert.Single(summaryProjectionService.UpdateCalls);
        Assert.Equal(1, commuteTripSyncService.SyncCalls);
        Assert.Equal(1, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
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
        FakeCityPopulationPersonReadRepository? personReadRepository = null,
        FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
        FakeCityPopulationWeatherExposureStateRepository? weatherExposureStateRepository = null,
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityEconomySettlementOutboxWriter? outboxWriter = null,
        FakeCityPopulationCommuteTripSyncService? commuteTripSyncService = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        var householdLivelihoodPolicy = new CityHouseholdLivelihoodPolicy();
        var householdCashflowPolicy = new CityHouseholdCashflowPolicy();
        var householdEconomyPolicy = new CityHouseholdEconomyPolicy(
            householdLivelihoodPolicy,
            householdCashflowPolicy);
        var anchorSelectionPolicy = new CityPopulationAnchorSelectionPolicy();

        return new AdvanceCityPopulationCommandHandler(
            personReadRepository: personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            cityPopulationArchiveStateRepository: new FakeCityPopulationArchiveStateRepository(),
            cityPopulationAnchorCatalogRepository: new FakeCityPopulationAnchorCatalogRepository(),
            cityPopulationCostOfLivingStateRepository: new FakeCityPopulationCostOfLivingStateRepository(),
            cityPopulationEssentialsStateRepository: new FakeCityPopulationEssentialsStateRepository(),
            cityPopulationServiceQualityStateRepository: new FakeCityPopulationServiceQualityStateRepository(),
            cityPopulationDeletionStateRepository: new FakeCityPopulationDeletionStateRepository(),
            employerFinancialStressStateRepository: new FakeCityPopulationEmployerFinancialStressStateRepository(),
            cityPopulationEnvironmentRepository: new FakeCityPopulationEnvironmentRepository(),
            householdFinancialStressStateRepository: new FakeCityPopulationHouseholdFinancialStressStateRepository(),
            cityPopulationLivingConditionsStateRepository: new FakeCityPopulationLivingConditionsStateRepository(),
            districtUtilityConditionsClient: new FakeCityDistrictUtilityConditionsClient(),
            commuteRoutingService: new FakeCityPopulationCommuteRoutingService(),
            commuteTripSyncService: commuteTripSyncService ?? new FakeCityPopulationCommuteTripSyncService(),
            cityPopulationActivityJournalService: activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            cityEconomySettlementOutboxWriter: outboxWriter ?? new FakeCityEconomySettlementOutboxWriter(),
            progressionStateRepository: progressionStateRepository ?? new FakeCityPopulationProgressionStateRepository(),
            cityPopulationSummaryProjectionService: summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            weatherExposureStateRepository: weatherExposureStateRepository ?? new FakeCityPopulationWeatherExposureStateRepository(),
            householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
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

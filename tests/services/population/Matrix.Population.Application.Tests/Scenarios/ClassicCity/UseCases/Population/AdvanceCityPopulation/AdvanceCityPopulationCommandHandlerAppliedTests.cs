using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandlerAppliedTests
    {
        [Fact]
        public async Task Handle_DateProgression_PublishesResidentHealthRiskInsteadOfApplyingIllnessLocally()
        {
            Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            Person resident = CreatePerson(
                personId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                householdId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                currentDate: new DateOnly(2048, 5, 5));
            Household household = Household.Create(
                id: resident.HouseholdId,
                size: HouseholdSize.From(1),
                createdAtUtc: UtcNow);
            var personRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [resident]
            };
            var householdRepository = new FakeHouseholdWriteRepository
            {
                HouseholdsByCityResult = [household],
                PlacementsByCityResult =
                [
                    ClassicCityHouseholdPlacement.CreateHomeless(
                        household.Id,
                        CityId.From(cityId))
                ]
            };
            var riskWriter = new FakePopulationResidentHealthRiskOutboxWriter();
            var anchorRepository = new FakeCityPopulationAnchorCatalogRepository();
            var healthcarePressureRepository = new FakeCityHealthcarePressureSnapshotRepository();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personRepository,
                householdWriteRepository: householdRepository,
                residentHealthRiskOutboxWriter: riskWriter,
                anchorRepository: anchorRepository,
                healthcarePressureRepository: healthcarePressureRepository);

            await handler.Handle(
                CreateCommand(cityId, tickId: 42),
                CancellationToken.None);

            var batch = Assert.Single(riskWriter.V2Batches);
            Assert.Equal(42, batch.SourceRevision);
            Assert.Equal(new DateOnly(2048, 5, 5), batch.PreviousDate);
            Assert.Equal(new DateOnly(2048, 5, 6), batch.CurrentDate);
            var risk = Assert.Single(batch.Residents);
            Assert.Equal(resident.Id.Value, risk.ResidentId);
            Assert.Equal("Unhoused", risk.HousingStability);
            Assert.Contains(
                anchorRepository.ListRequests,
                request => request.Type == CityAnchorType.Hospital);
            Assert.Equal(1, healthcarePressureRepository.GetByCityCalls);
        }

        [Fact]
        public async Task Handle_IntradayNeedsTick_SkipsHealthcareContextReads()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
            {
                State = CityPopulationProgressionState.Create(
                    cityId: CityId.From(cityId),
                    lastProcessedTickId: 42,
                    lastProcessedDate: new DateOnly(2048, 5, 6),
                    updatedAtUtc: UtcNow)
            };
            var anchorRepository = new FakeCityPopulationAnchorCatalogRepository();
            var healthcarePressureRepository = new FakeCityHealthcarePressureSnapshotRepository();
            var costOfLivingRepository = new FakeCityPopulationCostOfLivingStateRepository();
            var serviceQualityRepository = new FakeCityPopulationServiceQualityStateRepository();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                progressionStateRepository: progressionStateRepository,
                anchorRepository: anchorRepository,
                healthcarePressureRepository: healthcarePressureRepository,
                costOfLivingRepository: costOfLivingRepository,
                serviceQualityRepository: serviceQualityRepository);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: new AdvanceCityPopulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
                    TickId: 43),
                cancellationToken: CancellationToken.None);

            Assert.Equal(AdvanceCityPopulationStatus.Applied, result.Status);
            Assert.DoesNotContain(
                anchorRepository.ListRequests,
                request => request.Type == CityAnchorType.Hospital);
            Assert.Equal(0, healthcarePressureRepository.GetByCityCalls);
            Assert.Null(costOfLivingRepository.RequestedCityId);
            Assert.Null(serviceQualityRepository.RequestedCityId);
        }

        [Fact]
        public async Task Handle_WhenCityHasNoResidents_CreatesProgressionStateAndReturnsApplied()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = Array.Empty<Person>()
            };
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                HouseholdsByCityResult = Array.Empty<Household>(),
                PlacementsByCityResult = Array.Empty<ClassicCityHouseholdPlacement>()
            };
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var residentFactsOutboxWriter = new FakePopulationResidentFactsOutboxWriter();
            var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService();
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                progressionStateRepository: progressionStateRepository,
                householdWriteRepository: householdWriteRepository,
                summaryProjectionService: summaryProjectionService,
                activityJournalService: activityJournalService,
                outboxWriter: outboxWriter,
                residentFactsOutboxWriter: residentFactsOutboxWriter,
                commuteTripSyncService: commuteTripSyncService,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    tickId: 12),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Single(progressionStateRepository.AddedStates);
            Assert.NotNull(progressionStateRepository.State);
            Assert.Equal(
                expected: 12,
                actual: progressionStateRepository.State!.LastProcessedTickId);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                actual: progressionStateRepository.State.LastProcessedDate);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: personReadRepository.RequestedCityId);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: householdWriteRepository.RequestedCityId);
            Assert.Single(summaryProjectionService.UpdateCalls);
            Assert.Equal(
                expected: (CityId.From(cityId), new DateOnly(
                               year: 2048,
                               month: 5,
                               day: 6), 0, 0, true),
                actual: summaryProjectionService.UpdateCalls[0]);
            Assert.Empty(activityJournalService.Entries);
            Assert.Empty(outboxWriter.HouseholdBatches);
            Assert.Empty(outboxWriter.WorkplaceBatches);
            Assert.Empty(residentFactsOutboxWriter.Batches);
            Assert.Equal(
                expected: 1,
                actual: commuteTripSyncService.SyncCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_LethalNeedsPressure_DelegatesHealthWithoutPrematureLifecycleFact()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            Person resident = CreatePerson(
                personId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                householdId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                currentDate: new DateOnly(2048, 5, 5),
                energy: 0,
                stress: 100,
                socialNeed: 100,
                health: 1);
            Household household = Household.Create(
                id: resident.HouseholdId,
                size: HouseholdSize.From(1),
                createdAtUtc: UtcNow);
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [resident]
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                HouseholdsByCityResult = [household],
                PlacementsByCityResult =
                [
                    ClassicCityHouseholdPlacement.CreateHomeless(
                        householdId: household.Id,
                        cityId: CityId.From(cityId))
                ]
            };
            var residentFactsOutboxWriter = new FakePopulationResidentFactsOutboxWriter();
            var healthRiskOutboxWriter = new FakePopulationResidentHealthRiskOutboxWriter();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                householdWriteRepository: householdWriteRepository,
                residentFactsOutboxWriter: residentFactsOutboxWriter,
                residentHealthRiskOutboxWriter: healthRiskOutboxWriter);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    tickId: 41),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.Applied,
                actual: result.Status);
            Assert.True(resident.IsAlive);
            Assert.Empty(residentFactsOutboxWriter.Batches);
            var batch = Assert.Single(healthRiskOutboxWriter.V2Batches);
            Assert.Equal(
                expected: cityId,
                actual: batch.SimulationHostId);
            Assert.Equal(
                expected: 41,
                actual: batch.SourceRevision);
            var risk = Assert.Single(batch.Residents);
            Assert.Equal(
                expected: resident.Id.Value,
                actual: risk.ResidentId);
            Assert.True(risk.ExternalHealthDelta < 0);
        }

        [Fact]
        public async Task Handle_PendingWeatherImpact_JoinsHealthcareBatchAndIsDrainedAtomically()
        {
            Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateOnly currentDate = new(2048, 5, 6);
            DateTimeOffset currentSimTimeUtc = new(2048, 5, 6, 9, 0, 0, TimeSpan.Zero);
            Person resident = CreatePerson(
                personId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                householdId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                birthDate: new DateOnly(1960, 5, 6),
                currentDate: currentDate,
                health: 1);
            Household household = Household.Create(
                id: resident.HouseholdId,
                size: HouseholdSize.From(1),
                createdAtUtc: UtcNow);
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [resident]
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                HouseholdsByCityResult = [household],
                PlacementsByCityResult =
                [
                    ClassicCityHouseholdPlacement.CreateHomeless(
                        householdId: household.Id,
                        cityId: CityId.From(cityId))
                ]
            };
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
            {
                State = CityPopulationProgressionState.Create(
                    cityId: CityId.From(cityId),
                    lastProcessedTickId: 40,
                    lastProcessedDate: currentDate,
                    updatedAtUtc: UtcNow)
            };
            var pendingWeatherImpactRepository = new FakeCityPopulationPendingWeatherImpactRepository();
            pendingWeatherImpactRepository.Impacts.Add(
                CityPopulationPendingWeatherImpact.Create(
                    impactId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    cityId: CityId.From(cityId),
                    currentDate: currentDate,
                    previousWeather: new WeatherImpactProfile(
                        Type: PopulationWeatherType.Clear,
                        Severity: PopulationWeatherSeverity.Calm,
                        PrecipitationKind: PopulationPrecipitationKind.None,
                        TemperatureC: 22m,
                        HumidityPercent: 45m,
                        WindSpeedKph: 12m,
                        CloudCoveragePercent: 35m,
                        PressureHpa: 1012m),
                    currentWeather: new WeatherImpactProfile(
                        Type: PopulationWeatherType.Heatwave,
                        Severity: PopulationWeatherSeverity.Extreme,
                        PrecipitationKind: PopulationPrecipitationKind.None,
                        TemperatureC: 39m,
                        HumidityPercent: 45m,
                        WindSpeedKph: 12m,
                        CloudCoveragePercent: 35m,
                        PressureHpa: 1012m),
                    environment: null,
                    occurredAtUtc: currentSimTimeUtc));
            var healthRiskOutboxWriter = new FakePopulationResidentHealthRiskOutboxWriter();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                progressionStateRepository: progressionStateRepository,
                pendingWeatherImpactRepository: pendingWeatherImpactRepository,
                householdWriteRepository: householdWriteRepository,
                residentHealthRiskOutboxWriter: healthRiskOutboxWriter);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: new AdvanceCityPopulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: currentSimTimeUtc,
                    ToSimTimeUtc: currentSimTimeUtc,
                    TickId: 41),
                cancellationToken: CancellationToken.None);

            Assert.Equal(AdvanceCityPopulationStatus.Applied, result.Status);
            Assert.Equal(1, result.AffectedPeopleCount);
            Assert.True(resident.IsAlive);
            var batch = Assert.Single(healthRiskOutboxWriter.V2Batches);
            Assert.Equal(41, batch.SourceRevision);
            Assert.True(Assert.Single(batch.Residents).ExternalHealthDelta < 0);
            Assert.Empty(pendingWeatherImpactRepository.Impacts);
        }

        [Fact]
        public async Task Handle_WhenSameDayTickAdvancesOnlyState_MarksProgressWithoutResidentWork()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository();
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
            {
                State = CityPopulationProgressionState.Create(
                    cityId: CityId.From(cityId),
                    lastProcessedTickId: 12,
                    lastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 6),
                    updatedAtUtc: UtcNow)
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var residentFactsOutboxWriter = new FakePopulationResidentFactsOutboxWriter();
            var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService();
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                progressionStateRepository: progressionStateRepository,
                householdWriteRepository: householdWriteRepository,
                summaryProjectionService: summaryProjectionService,
                activityJournalService: activityJournalService,
                outboxWriter: outboxWriter,
                residentFactsOutboxWriter: residentFactsOutboxWriter,
                commuteTripSyncService: commuteTripSyncService,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: new AdvanceCityPopulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 13),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Empty(progressionStateRepository.AddedStates);
            Assert.NotNull(progressionStateRepository.State);
            Assert.Equal(
                expected: 13,
                actual: progressionStateRepository.State!.LastProcessedTickId);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                actual: progressionStateRepository.State.LastProcessedDate);
            Assert.Null(personReadRepository.RequestedCityId);
            Assert.Null(householdWriteRepository.RequestedCityId);
            Assert.Empty(summaryProjectionService.UpdateCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Empty(outboxWriter.HouseholdBatches);
            Assert.Empty(outboxWriter.WorkplaceBatches);
            Assert.Empty(residentFactsOutboxWriter.Batches);
            Assert.Equal(
                expected: 0,
                actual: commuteTripSyncService.SyncCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenWeatherExposureCheckpointAdvances_MarksExposureProcessed()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = Array.Empty<Person>()
            };
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
            {
                State = CityPopulationProgressionState.Create(
                    cityId: CityId.From(cityId),
                    lastProcessedTickId: 13,
                    lastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 6),
                    updatedAtUtc: UtcNow)
            };
            DateTimeOffset currentWeatherAt = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 8,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
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
                HouseholdsByCityResult = Array.Empty<Household>(),
                PlacementsByCityResult = Array.Empty<ClassicCityHouseholdPlacement>()
            };
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService();
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                progressionStateRepository: progressionStateRepository,
                weatherExposureStateRepository: weatherExposureStateRepository,
                householdWriteRepository: householdWriteRepository,
                summaryProjectionService: summaryProjectionService,
                commuteTripSyncService: commuteTripSyncService,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: new AdvanceCityPopulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: currentWeatherAt,
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 12,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 14),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.NotNull(weatherExposureStateRepository.State);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: weatherExposureStateRepository.State!.LastExposureProcessedAtSimTimeUtc);
            Assert.Equal(
                expected: 14,
                actual: progressionStateRepository.State!.LastProcessedTickId);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: personReadRepository.RequestedCityId);
            Assert.Single(summaryProjectionService.UpdateCalls);
            Assert.Equal(
                expected: 1,
                actual: commuteTripSyncService.SyncCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCommuteTripSyncFails_ReturnsApplied()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = Array.Empty<Person>()
            };
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                HouseholdsByCityResult = Array.Empty<Household>(),
                PlacementsByCityResult = Array.Empty<ClassicCityHouseholdPlacement>()
            };
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var commuteTripSyncService = new FakeCityPopulationCommuteTripSyncService
            {
                ExceptionToThrow = new InvalidOperationException("sync failed")
            };
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                progressionStateRepository: progressionStateRepository,
                householdWriteRepository: householdWriteRepository,
                summaryProjectionService: summaryProjectionService,
                commuteTripSyncService: commuteTripSyncService,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    tickId: 15),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Single(progressionStateRepository.AddedStates);
            Assert.Single(summaryProjectionService.UpdateCalls);
            Assert.Equal(
                expected: 1,
                actual: commuteTripSyncService.SyncCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static AdvanceCityPopulationCommand CreateCommand(
            Guid cityId,
            long tickId)
        {
            return new AdvanceCityPopulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 5,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                TickId: tickId);
        }

        private static AdvanceCityPopulationCommandHandler CreateHandler(
            FakeCityPopulationPersonReadRepository? personReadRepository = null,
            FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
            FakeCityPopulationPendingWeatherImpactRepository? pendingWeatherImpactRepository = null,
            FakeCityPopulationWeatherExposureStateRepository? weatherExposureStateRepository = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityEconomySettlementOutboxWriter? outboxWriter = null,
            FakePopulationResidentFactsOutboxWriter? residentFactsOutboxWriter = null,
            FakePopulationResidentHealthRiskOutboxWriter? residentHealthRiskOutboxWriter = null,
            FakePopulationResidentVitalStateOutboxWriter? residentVitalStateOutboxWriter = null,
            FakeCityPopulationCommuteTripSyncService? commuteTripSyncService = null,
            FakeCityPopulationAnchorCatalogRepository? anchorRepository = null,
            FakeCityHealthcarePressureSnapshotRepository? healthcarePressureRepository = null,
            FakeCityPopulationCostOfLivingStateRepository? costOfLivingRepository = null,
            FakeCityPopulationServiceQualityStateRepository? serviceQualityRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            var householdLivelihoodPolicy = new CityHouseholdLivelihoodPolicy();
            var householdCashflowPolicy = new CityHouseholdCashflowPolicy();
            var householdEconomyPolicy = new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: householdLivelihoodPolicy,
                householdCashflowPolicy: householdCashflowPolicy);
            var anchorSelectionPolicy = new CityPopulationAnchorSelectionPolicy();

            return new AdvanceCityPopulationCommandHandler(
                personReadRepository: personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
                cityPopulationArchiveStateRepository: new FakeCityPopulationArchiveStateRepository(),
                cityPopulationAnchorCatalogRepository: anchorRepository ??
                                                       new FakeCityPopulationAnchorCatalogRepository(),
                cityPopulationCostOfLivingStateRepository: costOfLivingRepository ??
                                                           new FakeCityPopulationCostOfLivingStateRepository(),
                cityPopulationEssentialsStateRepository: new FakeCityPopulationEssentialsStateRepository(),
                cityPopulationServiceQualityStateRepository: serviceQualityRepository ??
                                                             new FakeCityPopulationServiceQualityStateRepository(),
                healthcarePressureSnapshotRepository: healthcarePressureRepository ??
                                                      new FakeCityHealthcarePressureSnapshotRepository(),
                cityPopulationDeletionStateRepository: new FakeCityPopulationDeletionStateRepository(),
                employerFinancialStressStateRepository: new FakeCityPopulationEmployerFinancialStressStateRepository(),
                cityPopulationEnvironmentRepository: new FakeCityPopulationEnvironmentRepository(),
                householdFinancialStressStateRepository:
                new FakeCityPopulationHouseholdFinancialStressStateRepository(),
                cityPopulationLivingConditionsStateRepository: new FakeCityPopulationLivingConditionsStateRepository(),
                educationParticipationProjectionRepository: new FakeEducationParticipationProjectionRepository(),
                districtUtilityConditionsClient: new FakeCityDistrictUtilityConditionsClient(),
                commuteRoutingService: new FakeCityPopulationCommuteRoutingService(),
                commuteTripSyncService: commuteTripSyncService ?? new FakeCityPopulationCommuteTripSyncService(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityEconomySettlementOutboxWriter: outboxWriter ?? new FakeCityEconomySettlementOutboxWriter(),
                residentFactsOutboxWriter: residentFactsOutboxWriter ??
                                           new FakePopulationResidentFactsOutboxWriter(),
                residentHealthRiskOutboxWriter: residentHealthRiskOutboxWriter ??
                                                new FakePopulationResidentHealthRiskOutboxWriter(),
                residentVitalStateOutboxWriter: residentVitalStateOutboxWriter ??
                                                new FakePopulationResidentVitalStateOutboxWriter(),
                progressionStateRepository: progressionStateRepository ??
                                            new FakeCityPopulationProgressionStateRepository(),
                pendingWeatherImpactRepository: pendingWeatherImpactRepository ??
                                                new FakeCityPopulationPendingWeatherImpactRepository(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                weatherExposureStateRepository: weatherExposureStateRepository ??
                                                new FakeCityPopulationWeatherExposureStateRepository(),
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                marriageDomainService: new MarriageDomainService(),
                populationBirthDomainService: new PopulationBirthDomainService(),
                personWriteRepository: new FakePersonWriteRepository(),
                birthAutonomyPolicy: new CityBirthAutonomyPolicy(
                    contentCatalog: new TestPopulationGenerationContentCatalog(),
                    householdLivelihoodPolicy: householdLivelihoodPolicy),
                civilRegistryAutonomyPolicy: new CityCivilRegistryAutonomyPolicy(),
                employmentAutonomyPolicy: new CityEmploymentAutonomyPolicy(
                    contentCatalog: new TestPopulationGenerationContentCatalog(),
                    householdEconomyPolicy: householdEconomyPolicy,
                    anchorSelectionPolicy: anchorSelectionPolicy),
                householdLivelihoodPolicy: householdLivelihoodPolicy,
                householdCashflowPolicy: householdCashflowPolicy,
                householdPressurePolicy: new CityHouseholdPressurePolicy(),
                housingAutonomyPolicy: new CityHousingAutonomyPolicy(householdEconomyPolicy),
                householdIndependenceAutonomyPolicy: new CityHouseholdIndependenceAutonomyPolicy(
                    householdLivelihoodPolicy),
                anchorSelectionPolicy: anchorSelectionPolicy,
                districtImpactPolicy: new CityPopulationDistrictImpactPolicy(),
                livingConditionsPressurePolicy: new CityPopulationLivingConditionsPressurePolicy(),
                participationPolicy: new CityPopulationParticipationPolicy(),
                personNeedsProgressionPolicy: new PersonNeedsProgressionPolicy(),
                weatherImpactPolicy: new CityPopulationWeatherImpactPolicy(
                    new CityPopulationClimateAdaptationPolicy()),
                weatherExposurePolicy: new CityPopulationWeatherExposurePolicy(
                    new CityPopulationClimateAdaptationPolicy()),
                timeProvider: CreateTimeProvider(),
                logger: NullLogger<AdvanceCityPopulationCommandHandler>.Instance,
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
            public IReadOnlyList<string> FemaleFirstNames => ["Anna"];

            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
            [
                new(
                    Masculine: "Ivanov",
                    Feminine: "Ivanova")
            ];

            public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
            [
                new(
                    Title: "Engineer",
                    Weight: 1)
            ];
        }
    }
}

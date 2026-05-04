using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress;

public sealed class ApplyCityHouseholdFinancialStressCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
    {
        var processedRepository = new FakeProcessedIntegrationMessageRepository
        {
            TryMarkProcessedResult = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            processedRepository: processedRepository,
            unitOfWork: unitOfWork);

        ApplyCityHouseholdFinancialStressResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityHouseholdFinancialStressStatus.Duplicate, result.Status);
        Assert.Equal(0, result.AppliedHouseholdCount);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var stateRepository = new FakeCityPopulationHouseholdFinancialStressStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            stateRepository: stateRepository);

        ApplyCityHouseholdFinancialStressResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityHouseholdFinancialStressStatus.CityDeleted, result.Status);
        Assert.Equal(0, result.AppliedHouseholdCount);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenCityIsArchived_ReturnsArchivedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
        {
            State = CityPopulationArchiveState.Create(
                cityId: CityId.From(cityId),
                archivedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var stateRepository = new FakeCityPopulationHouseholdFinancialStressStateRepository();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            stateRepository: stateRepository);

        ApplyCityHouseholdFinancialStressResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityHouseholdFinancialStressStatus.CityArchived, result.Status);
        Assert.Equal(0, result.AppliedHouseholdCount);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenPayloadContainsInvalidAndStaleHouseholds_AppliesOnlyFreshEntries()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid staleHouseholdGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid updatedHouseholdGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid newHouseholdGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var stateRepository = new FakeCityPopulationHouseholdFinancialStressStateRepository();
        CityPopulationHouseholdFinancialStressState staleState = CityPopulationHouseholdFinancialStressState.Create(
            cityId: CityId.From(cityId),
            householdId: HouseholdId.From(staleHouseholdGuid),
            overdueObligationCount: 1,
            overdueRentCount: 0,
            overdueUtilityCount: 1,
            arrearsObligationCount: 0,
            serviceCutoffCount: 0,
            evictionNoticeCount: 0,
            evictionEligibleCount: 0,
            oldestOverdueAgeDays: 12,
            totalOverdueAmount: 450m,
            distressScore: 0.25m,
            lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 30, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 31, 0, TimeSpan.Zero));
        CityPopulationHouseholdFinancialStressState updatedState = CityPopulationHouseholdFinancialStressState.Create(
            cityId: CityId.From(cityId),
            householdId: HouseholdId.From(updatedHouseholdGuid),
            overdueObligationCount: 0,
            overdueRentCount: 0,
            overdueUtilityCount: 0,
            arrearsObligationCount: 0,
            serviceCutoffCount: 0,
            evictionNoticeCount: 0,
            evictionEligibleCount: 0,
            oldestOverdueAgeDays: 0,
            totalOverdueAmount: 0m,
            distressScore: 0.10m,
            lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 4, 11, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 4, 11, 1, 0, TimeSpan.Zero));
        stateRepository.States.Add(staleState);
        stateRepository.States.Add(updatedState);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityHouseholdFinancialStressResult result = await handler.Handle(
            CreateCommand(
                households:
                [
                    new HouseholdFinancialStressSnapshotInput(
                        HouseholdExternalReferenceCode: "broken-household-ref",
                        OverdueObligationCount: 1,
                        OverdueRentCount: 1,
                        OverdueUtilityCount: 0,
                        ArrearsObligationCount: 1,
                        ServiceCutoffCount: 0,
                        EvictionNoticeCount: 0,
                        EvictionEligibleCount: 0,
                        OldestOverdueAgeDays: 20,
                        TotalOverdueAmount: 350m,
                        DistressScore: 0.55m),
                    new HouseholdFinancialStressSnapshotInput(
                        HouseholdExternalReferenceCode: $"classic-city-household:{staleHouseholdGuid:N}",
                        OverdueObligationCount: 3,
                        OverdueRentCount: 2,
                        OverdueUtilityCount: 1,
                        ArrearsObligationCount: 2,
                        ServiceCutoffCount: 1,
                        EvictionNoticeCount: 1,
                        EvictionEligibleCount: 1,
                        OldestOverdueAgeDays: 40,
                        TotalOverdueAmount: 950m,
                        DistressScore: 0.90m),
                    new HouseholdFinancialStressSnapshotInput(
                        HouseholdExternalReferenceCode: $"classic-city-household:{updatedHouseholdGuid:N}",
                        OverdueObligationCount: 1,
                        OverdueRentCount: 1,
                        OverdueUtilityCount: 0,
                        ArrearsObligationCount: 1,
                        ServiceCutoffCount: 0,
                        EvictionNoticeCount: 1,
                        EvictionEligibleCount: 0,
                        OldestOverdueAgeDays: 15,
                        TotalOverdueAmount: 280m,
                        DistressScore: 0.35m),
                    new HouseholdFinancialStressSnapshotInput(
                        HouseholdExternalReferenceCode: $"classic-city-household:{newHouseholdGuid:N}",
                        OverdueObligationCount: 2,
                        OverdueRentCount: 1,
                        OverdueUtilityCount: 1,
                        ArrearsObligationCount: 1,
                        ServiceCutoffCount: 1,
                        EvictionNoticeCount: 1,
                        EvictionEligibleCount: 0,
                        OldestOverdueAgeDays: 22,
                        TotalOverdueAmount: 610m,
                        DistressScore: 0.62m)
                ]),
            CancellationToken.None);

        Assert.Equal(ApplyCityHouseholdFinancialStressStatus.Applied, result.Status);
        Assert.Equal(2, result.AppliedHouseholdCount);
        CityPopulationHouseholdFinancialStressState addedState = Assert.Single(stateRepository.AddedStates);
        Assert.Equal(HouseholdId.From(newHouseholdGuid), addedState.HouseholdId);
        Assert.Equal(610m, addedState.TotalOverdueAmount);
        Assert.Equal(0.35m, updatedState.DistressScore);
        Assert.Equal(1, updatedState.EvictionNoticeCount);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero), updatedState.LastEvaluatedAtUtc);
        Assert.Equal(0.25m, staleState.DistressScore);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityHouseholdFinancialStressCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationHouseholdFinancialStressStateRepository? stateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityHouseholdFinancialStressCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            stateRepository ?? new FakeCityPopulationHouseholdFinancialStressStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityHouseholdFinancialStressCommand CreateCommand(
        IReadOnlyList<HouseholdFinancialStressSnapshotInput>? households = null)
    {
        return new ApplyCityHouseholdFinancialStressCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-household-stress",
            OccurredAtUtc: new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero),
            Households: households ??
                [
                    new HouseholdFinancialStressSnapshotInput(
                        HouseholdExternalReferenceCode: "classic-city-household:11111111111111111111111111111111",
                        OverdueObligationCount: 1,
                        OverdueRentCount: 0,
                        OverdueUtilityCount: 1,
                        ArrearsObligationCount: 1,
                        ServiceCutoffCount: 0,
                        EvictionNoticeCount: 0,
                        EvictionEligibleCount: 0,
                        OldestOverdueAgeDays: 10,
                        TotalOverdueAmount: 220m,
                        DistressScore: 0.40m)
                ]);
    }
}

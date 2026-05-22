using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress;

public sealed class ApplyCityEmployerFinancialStressCommandHandlerTests
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

        ApplyCityEmployerFinancialStressResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEmployerFinancialStressStatus.Duplicate, result.Status);
        Assert.Equal(0, result.AppliedEmployerCount);
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
        var stateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            stateRepository: stateRepository);

        ApplyCityEmployerFinancialStressResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEmployerFinancialStressStatus.CityDeleted, result.Status);
        Assert.Equal(0, result.AppliedEmployerCount);
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
        var stateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            stateRepository: stateRepository);

        ApplyCityEmployerFinancialStressResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEmployerFinancialStressStatus.CityArchived, result.Status);
        Assert.Equal(0, result.AppliedEmployerCount);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenPayloadContainsInvalidAndStaleEmployers_AppliesOnlyFreshEntries()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid staleWorkplaceGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid updatedWorkplaceGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid newWorkplaceGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var stateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
        CityPopulationEmployerFinancialStressState staleState = CityPopulationEmployerFinancialStressState.Create(
            cityId: CityId.From(cityId),
            workplaceId: WorkplaceId.From(staleWorkplaceGuid),
            requestedGrossPayrollAmount: 12000m,
            paidGrossPayrollAmount: 11800m,
            missedGrossPayrollAmount: 200m,
            payrollFulfillmentRatio: 0.9833m,
            failedPayrollCount: 0,
            partialPayrollCount: 1,
            currentBalanceAmount: 500m,
            distressScore: 0.15m,
            hasHiringFreeze: false,
            hasLayoffPressure: false,
            lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 30, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 31, 0, TimeSpan.Zero));
        CityPopulationEmployerFinancialStressState updatedState = CityPopulationEmployerFinancialStressState.Create(
            cityId: CityId.From(cityId),
            workplaceId: WorkplaceId.From(updatedWorkplaceGuid),
            requestedGrossPayrollAmount: 8000m,
            paidGrossPayrollAmount: 8000m,
            missedGrossPayrollAmount: 0m,
            payrollFulfillmentRatio: 1m,
            failedPayrollCount: 0,
            partialPayrollCount: 0,
            currentBalanceAmount: 950m,
            distressScore: 0.05m,
            hasHiringFreeze: false,
            hasLayoffPressure: false,
            lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 4, 11, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 4, 11, 1, 0, TimeSpan.Zero));
        stateRepository.States.Add(staleState);
        stateRepository.States.Add(updatedState);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityEmployerFinancialStressResult result = await handler.Handle(
            CreateCommand(
                employers:
                [
                    new EmployerFinancialStressSnapshotInput(
                        WorkplaceExternalReferenceCode: "broken-workplace-ref",
                        RequestedGrossPayrollAmount: 10000m,
                        PaidGrossPayrollAmount: 9000m,
                        MissedGrossPayrollAmount: 1000m,
                        PayrollFulfillmentRatio: 0.90m,
                        FailedPayrollCount: 1,
                        PartialPayrollCount: 0,
                        CurrentBalanceAmount: -100m,
                        DistressScore: 0.70m,
                        HasHiringFreeze: true,
                        HasLayoffPressure: true),
                    new EmployerFinancialStressSnapshotInput(
                        WorkplaceExternalReferenceCode: $"classic-city-workplace:{staleWorkplaceGuid:N}",
                        RequestedGrossPayrollAmount: 15000m,
                        PaidGrossPayrollAmount: 10000m,
                        MissedGrossPayrollAmount: 5000m,
                        PayrollFulfillmentRatio: 0.6667m,
                        FailedPayrollCount: 2,
                        PartialPayrollCount: 1,
                        CurrentBalanceAmount: -500m,
                        DistressScore: 0.80m,
                        HasHiringFreeze: true,
                        HasLayoffPressure: true),
                    new EmployerFinancialStressSnapshotInput(
                        WorkplaceExternalReferenceCode: $"classic-city-workplace:{updatedWorkplaceGuid:N}",
                        RequestedGrossPayrollAmount: 9000m,
                        PaidGrossPayrollAmount: 8700m,
                        MissedGrossPayrollAmount: 300m,
                        PayrollFulfillmentRatio: 0.9667m,
                        FailedPayrollCount: 0,
                        PartialPayrollCount: 1,
                        CurrentBalanceAmount: 250m,
                        DistressScore: 0.22m,
                        HasHiringFreeze: false,
                        HasLayoffPressure: true),
                    new EmployerFinancialStressSnapshotInput(
                        WorkplaceExternalReferenceCode: $"classic-city-workplace:{newWorkplaceGuid:N}",
                        RequestedGrossPayrollAmount: 11000m,
                        PaidGrossPayrollAmount: 10000m,
                        MissedGrossPayrollAmount: 1000m,
                        PayrollFulfillmentRatio: 0.9091m,
                        FailedPayrollCount: 1,
                        PartialPayrollCount: 1,
                        CurrentBalanceAmount: -220m,
                        DistressScore: 0.65m,
                        HasHiringFreeze: true,
                        HasLayoffPressure: true)
                ]),
            CancellationToken.None);

        Assert.Equal(ApplyCityEmployerFinancialStressStatus.Applied, result.Status);
        Assert.Equal(2, result.AppliedEmployerCount);
        CityPopulationEmployerFinancialStressState addedState = Assert.Single(stateRepository.AddedStates);
        Assert.Equal(WorkplaceId.From(newWorkplaceGuid), addedState.WorkplaceId);
        Assert.Equal(0.9091m, addedState.PayrollFulfillmentRatio);
        Assert.Equal(0.22m, updatedState.DistressScore);
        Assert.True(updatedState.HasLayoffPressure);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero), updatedState.LastEvaluatedAtUtc);
        Assert.Equal(0.15m, staleState.DistressScore);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityEmployerFinancialStressCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationEmployerFinancialStressStateRepository? stateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityEmployerFinancialStressCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            stateRepository ?? new FakeCityPopulationEmployerFinancialStressStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityEmployerFinancialStressCommand CreateCommand(
        IReadOnlyList<EmployerFinancialStressSnapshotInput>? employers = null)
    {
        return new ApplyCityEmployerFinancialStressCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-employer-stress",
            OccurredAtUtc: new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero),
            Employers: employers ??
                [
                    new EmployerFinancialStressSnapshotInput(
                        WorkplaceExternalReferenceCode: "classic-city-workplace:11111111111111111111111111111111",
                        RequestedGrossPayrollAmount: 10000m,
                        PaidGrossPayrollAmount: 9200m,
                        MissedGrossPayrollAmount: 800m,
                        PayrollFulfillmentRatio: 0.92m,
                        FailedPayrollCount: 1,
                        PartialPayrollCount: 0,
                        CurrentBalanceAmount: -150m,
                        DistressScore: 0.55m,
                        HasHiringFreeze: true,
                        HasLayoffPressure: false)
                ]);
    }
}

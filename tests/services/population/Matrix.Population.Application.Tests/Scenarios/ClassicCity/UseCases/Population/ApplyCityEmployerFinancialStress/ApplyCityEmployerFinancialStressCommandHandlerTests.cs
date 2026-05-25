using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress
{
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
            ApplyCityEmployerFinancialStressCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ApplyCityEmployerFinancialStressResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEmployerFinancialStressStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AppliedEmployerCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var stateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
            ApplyCityEmployerFinancialStressCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                stateRepository: stateRepository);

            ApplyCityEmployerFinancialStressResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEmployerFinancialStressStatus.CityDeleted,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AppliedEmployerCount);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenCityIsArchived_ReturnsArchivedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var stateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
            ApplyCityEmployerFinancialStressCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                stateRepository: stateRepository);

            ApplyCityEmployerFinancialStressResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEmployerFinancialStressStatus.CityArchived,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AppliedEmployerCount);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenPayloadContainsInvalidAndStaleEmployers_AppliesOnlyFreshEntries()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var staleWorkplaceGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var updatedWorkplaceGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var newWorkplaceGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var stateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
            var staleState = CityPopulationEmployerFinancialStressState.Create(
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
                lastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 31,
                    second: 0,
                    offset: TimeSpan.Zero));
            var updatedState = CityPopulationEmployerFinancialStressState.Create(
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
                lastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 11,
                    minute: 1,
                    second: 0,
                    offset: TimeSpan.Zero));
            stateRepository.States.Add(staleState);
            stateRepository.States.Add(updatedState);
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityEmployerFinancialStressCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityEmployerFinancialStressResult result = await handler.Handle(
                request: CreateCommand(
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
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityEmployerFinancialStressStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 2,
                actual: result.AppliedEmployerCount);
            CityPopulationEmployerFinancialStressState addedState = Assert.Single(stateRepository.AddedStates);
            Assert.Equal(
                expected: WorkplaceId.From(newWorkplaceGuid),
                actual: addedState.WorkplaceId);
            Assert.Equal(
                expected: 0.9091m,
                actual: addedState.PayrollFulfillmentRatio);
            Assert.Equal(
                expected: 0.22m,
                actual: updatedState.DistressScore);
            Assert.True(updatedState.HasLayoffPressure);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: updatedState.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: 0.15m,
                actual: staleState.DistressScore);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static ApplyCityEmployerFinancialStressCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationEmployerFinancialStressStateRepository? stateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyCityEmployerFinancialStressCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                employerFinancialStressStateRepository: stateRepository ??
                                                        new FakeCityPopulationEmployerFinancialStressStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ApplyCityEmployerFinancialStressCommand CreateCommand(
            IReadOnlyList<EmployerFinancialStressSnapshotInput>? employers = null)
        {
            return new ApplyCityEmployerFinancialStressCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-employer-stress",
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
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
}

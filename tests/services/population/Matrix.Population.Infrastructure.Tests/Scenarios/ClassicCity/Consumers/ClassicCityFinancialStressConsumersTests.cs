using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityFinancialStressConsumersTests
    {
        [Fact]
        public async Task EmployerFinancialStressConsumer_WhenApplied_MapsBatchAndLogsInformation()
        {
            var mediator = new StressMediator
            {
                EmployerResult = new ApplyCityEmployerFinancialStressResult(
                    Status: ApplyCityEmployerFinancialStressStatus.Applied,
                    AppliedEmployerCount: 1)
            };
            var logger = new TestLogger<ClassicCityEmployerFinancialStressConsumer>();
            var consumer = new ClassicCityEmployerFinancialStressConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("4678b8c8-6afc-4271-8628-3f3322f245e2");
            ClassicCityEmployerFinancialStressBatchV1 message = new(
                CityId: Guid.Parse("bc4a843f-4c77-4a6a-bc8f-af7cc231ac2c"),
                BatchNumber: 2,
                TotalBatches: 5,
                Employers:
                [
                    new ClassicCityEmployerFinancialStressItemV1(
                        EmployerBusinessId: Guid.Parse("01aa05f5-0a32-4b32-a8d2-67d6d1265ccb"),
                        WorkplaceExternalReferenceCode: "wp-101",
                        RequestedGrossPayrollAmount: 1000m,
                        PaidGrossPayrollAmount: 800m,
                        MissedGrossPayrollAmount: 200m,
                        PayrollFulfillmentRatio: 0.8m,
                        FailedPayrollCount: 1,
                        PartialPayrollCount: 2,
                        CurrentBalanceAmount: 150m,
                        DistressScore: 0.7m,
                        HasHiringFreeze: true,
                        HasLayoffPressure: false)
                ],
                CorrelationId: "corr:stress",
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 16,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            await consumer.ConsumeAsync(
                message: message,
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            ApplyCityEmployerFinancialStressCommand command = Assert.Single(mediator.EmployerCommands);
            EmployerFinancialStressSnapshotInput employer = Assert.Single(command.Employers);
            Assert.Equal(
                expected: "wp-101",
                actual: employer.WorkplaceExternalReferenceCode);
            Assert.Equal(
                expected: 0.8m,
                actual: employer.PayrollFulfillmentRatio);
            Assert.True(employer.HasHiringFreeze);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "batch=2/5",
                actualString: entry.Message);
        }

        [Fact]
        public async Task EmployerFinancialStressConsumer_WhenMessageIdIsMissing_ThrowsInvalidOperationException()
        {
            var consumer = new ClassicCityEmployerFinancialStressConsumer(
                mediator: new StressMediator(),
                logger: new TestLogger<ClassicCityEmployerFinancialStressConsumer>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ConsumeAsync(
                message: new ClassicCityEmployerFinancialStressBatchV1(
                    CityId: Guid.Parse("bc4a843f-4c77-4a6a-bc8f-af7cc231ac2c"),
                    BatchNumber: 1,
                    TotalBatches: 1,
                    Employers: [],
                    CorrelationId: "corr:stress",
                    OccurredAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 16,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                messageId: null,
                cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task HouseholdFinancialStressConsumer_WhenCityArchived_LogsDebugAndMapsItems()
        {
            var mediator = new StressMediator
            {
                HouseholdResult = new ApplyCityHouseholdFinancialStressResult(
                    Status: ApplyCityHouseholdFinancialStressStatus.CityArchived,
                    AppliedHouseholdCount: 0)
            };
            var logger = new TestLogger<ClassicCityHouseholdFinancialStressConsumer>();
            var consumer = new ClassicCityHouseholdFinancialStressConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("07042ec8-caa1-495d-a1c8-86232e18369b");
            ClassicCityHouseholdFinancialStressBatchV1 message = new(
                CityId: Guid.Parse("3f3d50d0-4d3d-45fb-91bb-db870d8058e4"),
                BatchNumber: 1,
                TotalBatches: 2,
                Households:
                [
                    new ClassicCityHouseholdFinancialStressItemV1(
                        HouseholdAccountId: Guid.Parse("f3339d01-b7dd-47d5-b89c-7f3a2b4fe2c5"),
                        HouseholdExternalReferenceCode: "hh-201",
                        OverdueObligationCount: 2,
                        OverdueRentCount: 1,
                        OverdueUtilityCount: 3,
                        ArrearsObligationCount: 1,
                        ServiceCutoffCount: 0,
                        EvictionNoticeCount: 1,
                        EvictionEligibleCount: 0,
                        OldestOverdueAgeDays: 14,
                        TotalOverdueAmount: 250m,
                        DistressScore: 0.6m)
                ],
                CorrelationId: "corr:households",
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 16,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero));

            await consumer.ConsumeAsync(
                message: message,
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            ApplyCityHouseholdFinancialStressCommand command = Assert.Single(mediator.HouseholdCommands);
            HouseholdFinancialStressSnapshotInput household = Assert.Single(command.Households);
            Assert.Equal(
                expected: "hh-201",
                actual: household.HouseholdExternalReferenceCode);
            Assert.Equal(
                expected: 14,
                actual: household.OldestOverdueAgeDays);
            Assert.Equal(
                expected: 250m,
                actual: household.TotalOverdueAmount);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "archived",
                actualString: entry.Message);
        }

        private sealed class StressMediator : IMediator
        {
            public List<ApplyCityEmployerFinancialStressCommand> EmployerCommands { get; } = [];
            public List<ApplyCityHouseholdFinancialStressCommand> HouseholdCommands { get; } = [];

            public ApplyCityEmployerFinancialStressResult EmployerResult { get; init; } =
                new(
                    Status: ApplyCityEmployerFinancialStressStatus.Duplicate,
                    AppliedEmployerCount: 0);

            public ApplyCityHouseholdFinancialStressResult HouseholdResult { get; init; } =
                new(
                    Status: ApplyCityHouseholdFinancialStressStatus.Duplicate,
                    AppliedHouseholdCount: 0);

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                switch (request)
                {
                    case ApplyCityEmployerFinancialStressCommand employerCommand:
                        EmployerCommands.Add(employerCommand);
                        return Task.FromResult((TResponse)(object)EmployerResult);
                    case ApplyCityHouseholdFinancialStressCommand householdCommand:
                        HouseholdCommands.Add(householdCommand);
                        return Task.FromResult((TResponse)(object)HouseholdResult);
                    default:
                        throw new NotSupportedException();
                }
            }

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                throw new NotSupportedException();
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                throw new NotSupportedException();
            }
        }
    }
}

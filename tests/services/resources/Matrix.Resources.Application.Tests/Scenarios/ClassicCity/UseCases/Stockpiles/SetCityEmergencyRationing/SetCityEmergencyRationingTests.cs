using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing
{
    public sealed class SetCityEmergencyRationingTests
    {
        [Fact]
        public void Validator_RejectsEmptyCityId()
        {
            var validator = new SetCityEmergencyRationingCommandValidator();

            ValidationResult? result = validator.Validate(
                new SetCityEmergencyRationingCommand(
                    CityId: Guid.Empty,
                    Enabled: true));

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
        {
            var handler = new SetCityEmergencyRationingCommandHandler(
                repository: new FakeCityStockpileRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityStockpileSnapshotOutboxWriter(),
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            SetCityEmergencyRationingResult result = await handler.Handle(
                request: new SetCityEmergencyRationingCommand(
                    CityId: CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SetCityEmergencyRationingStatus.NotInitialized,
                actual: result.Status);
        }

        [Fact]
        public async Task Handler_ReturnsDuplicateWhenRequestedFlagMatchesCurrentState()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState(emergencyRationingEnabled: true)
            };
            var handler = new SetCityEmergencyRationingCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityStockpileSnapshotOutboxWriter(),
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            SetCityEmergencyRationingResult result = await handler.Handle(
                request: new SetCityEmergencyRationingCommand(
                    CityId: CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SetCityEmergencyRationingStatus.Duplicate,
                actual: result.Status);
            Assert.True(result.EmergencyRationingEnabled);
        }

        [Fact]
        public async Task Handler_AppliesRationingAndWritesSnapshotWithInjectedTime()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState(emergencyRationingEnabled: false)
            };
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
            DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(30);
            var handler = new SetCityEmergencyRationingCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider(occurredAtUtc));

            SetCityEmergencyRationingResult result = await handler.Handle(
                request: new SetCityEmergencyRationingCommand(
                    CityId: CityId,
                    Enabled: true),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SetCityEmergencyRationingStatus.Applied,
                actual: result.Status);
            Assert.True(repository.State!.EmergencyRationingEnabled);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Single(outboxWriter.Snapshots);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: outboxWriter.Snapshots[0].OccurredAtUtc);
        }
    }
}

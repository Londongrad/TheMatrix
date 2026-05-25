using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.UpdateCityEnvironment
{
    public sealed class UpdateCityEnvironmentCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var handler = new UpdateCityEnvironmentCommandHandler(
                cityRepository: cityRepository,
                outboxWriter: outboxWriter,
                unitOfWork: unitOfWork);

            bool result = await handler.Handle(
                request: new UpdateCityEnvironmentCommand(
                    CityId: Guid.NewGuid(),
                    ClimateZone: "Temperate",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 180),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Empty(outboxWriter.CityEvents);
        }

        [Fact]
        public async Task Handle_WhenCityExists_ChangesEnvironmentPublishesEventAndSaves()
        {
            City city = ClassicCityTestSupport.CreateCity();
            city.ClearDomainEvents();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var handler = new UpdateCityEnvironmentCommandHandler(
                cityRepository: cityRepository,
                outboxWriter: outboxWriter,
                unitOfWork: unitOfWork);

            bool result = await handler.Handle(
                request: new UpdateCityEnvironmentCommand(
                    CityId: city.Id.Value,
                    ClimateZone: "Arid",
                    Hemisphere: "Southern",
                    UtcOffsetMinutes: -120),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: "Arid",
                actual: city.Environment.ClimateZone.ToString());
            Assert.Equal(
                expected: "Southern",
                actual: city.Environment.Hemisphere.ToString());
            Assert.Equal(
                expected: -120,
                actual: city.Environment.UtcOffset.TotalMinutes);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            IDomainEvent domainEvent = Assert.Single(outboxWriter.CityEvents);
            Assert.IsType<CityEnvironmentChangedDomainEvent>(domainEvent);
        }
    }
}

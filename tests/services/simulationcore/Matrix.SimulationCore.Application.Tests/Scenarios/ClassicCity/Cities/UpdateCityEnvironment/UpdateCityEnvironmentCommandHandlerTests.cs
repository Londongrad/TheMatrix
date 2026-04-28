using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.UpdateCityEnvironment;

public sealed class UpdateCityEnvironmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new UpdateCityEnvironmentCommandHandler(cityRepository, outboxWriter, unitOfWork);

        var result = await handler.Handle(
            new UpdateCityEnvironmentCommand(Guid.NewGuid(), "Temperate", "Northern", 180),
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(outboxWriter.CityEvents);
    }

    [Fact]
    public async Task Handle_WhenCityExists_ChangesEnvironmentPublishesEventAndSaves()
    {
        var city = ClassicCityTestSupport.CreateCity();
        city.ClearDomainEvents();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new UpdateCityEnvironmentCommandHandler(cityRepository, outboxWriter, unitOfWork);

        var result = await handler.Handle(
            new UpdateCityEnvironmentCommand(city.Id.Value, "Arid", "Southern", -120),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal("Arid", city.Environment.ClimateZone.ToString());
        Assert.Equal("Southern", city.Environment.Hemisphere.ToString());
        Assert.Equal(-120, city.Environment.UtcOffset.TotalMinutes);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        var domainEvent = Assert.Single(outboxWriter.CityEvents);
        Assert.IsType<CityEnvironmentChangedDomainEvent>(domainEvent);
    }
}

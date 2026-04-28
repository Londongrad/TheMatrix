using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.DeleteCity;

public sealed class DeleteCityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsNotFound()
    {
        var handler = new DeleteCityCommandHandler(
            new ClassicCityTestSupport.FakeCityRepository(),
            new SimulationTestSupport.FakeSimulationClockRepository(),
            new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-01T10:00:00+00:00")));

        var result = await handler.Handle(new DeleteCityCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(DeleteCityResult.NotFound, result);
    }

    [Fact]
    public async Task Handle_WhenCityIsNotArchived_ReturnsNotAllowed()
    {
        var city = ClassicCityTestSupport.CreateCity();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var handler = new DeleteCityCommandHandler(
            cityRepository,
            new SimulationTestSupport.FakeSimulationClockRepository(),
            new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-01T10:00:00+00:00")));

        var result = await handler.Handle(new DeleteCityCommand(city.Id.Value), CancellationToken.None);

        Assert.Equal(DeleteCityResult.NotAllowed, result);
    }

    [Fact]
    public async Task Handle_WhenCityIsArchived_DeletesClockWritesOutboxAndRemovesCity()
    {
        DateTimeOffset deletedAtUtc = DateTimeOffset.Parse("2048-06-01T10:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity();
        city.Archive(deletedAtUtc.AddHours(-1));
        city.ClearDomainEvents();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new DeleteCityCommandHandler(
            cityRepository,
            clockRepository,
            outboxWriter,
            unitOfWork,
            new ApplicationTestSupport.FixedTimeProvider(deletedAtUtc));

        var result = await handler.Handle(new DeleteCityCommand(city.Id.Value), CancellationToken.None);

        Assert.Equal(DeleteCityResult.Deleted, result);
        Assert.Equal(1, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(city.Id.Value, clockRepository.DeletedSimulationId!.Value.Value);
        Assert.Same(city, cityRepository.DeletedCity);
        var domainEvent = Assert.Single(outboxWriter.CityEvents);
        var deletedEvent = Assert.IsType<CityDeletedDomainEvent>(domainEvent);
        Assert.Equal(city.Id, deletedEvent.CityId);
        Assert.Equal(deletedAtUtc, deletedEvent.DeletedAtUtc);
    }
}

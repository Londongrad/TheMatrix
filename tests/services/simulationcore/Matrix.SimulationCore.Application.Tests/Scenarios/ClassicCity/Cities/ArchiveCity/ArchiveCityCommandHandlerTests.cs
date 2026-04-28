using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ArchiveCity;

public sealed class ArchiveCityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var handler = new ArchiveCityCommandHandler(
            cityRepository,
            new SimulationTestSupport.FakeSimulationClockMutationExecutor(),
            new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-01T10:00:00+00:00")));

        var result = await handler.Handle(new ArchiveCityCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WhenCityIsActive_PausesClockArchivesCityPublishesEventAndSaves()
    {
        DateTimeOffset archivedAtUtc = DateTimeOffset.Parse("2048-06-01T10:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity();
        city.ClearDomainEvents();
        var clock = SimulationTestSupport.CreateClock(city.Id.Value, ClockState.Running);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var mutationExecutor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
        {
            Clock = clock,
            Result = true
        };
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var timeProvider = new ApplicationTestSupport.FixedTimeProvider(archivedAtUtc);
        var handler = new ArchiveCityCommandHandler(cityRepository, mutationExecutor, outboxWriter, unitOfWork, timeProvider);

        var result = await handler.Handle(new ArchiveCityCommand(city.Id.Value), CancellationToken.None);

        Assert.True(result);
        Assert.True(city.IsArchived);
        Assert.Equal(archivedAtUtc, city.ArchivedAtUtc);
        Assert.Equal(ClockState.Paused, clock.State);
        Assert.Equal(city.Id.Value, mutationExecutor.RequestedSimulationId!.Value.Value);
        Assert.True(mutationExecutor.RequestedAllowArchivedHost);
        Assert.Equal(2, cityRepository.GetByIdCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        var domainEvent = Assert.Single(outboxWriter.CityEvents);
        var archivedEvent = Assert.IsType<CityArchivedDomainEvent>(domainEvent);
        Assert.Equal(city.Id, archivedEvent.CityId);
        Assert.Equal(archivedAtUtc, archivedEvent.ArchivedAtUtc);
    }
}

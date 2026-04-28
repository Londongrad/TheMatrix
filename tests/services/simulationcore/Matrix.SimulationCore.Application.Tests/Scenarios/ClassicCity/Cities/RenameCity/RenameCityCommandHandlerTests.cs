using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RenameCity;

public sealed class RenameCityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new RenameCityCommandHandler(cityRepository, unitOfWork);

        var result = await handler.Handle(new RenameCityCommand(Guid.NewGuid(), "Renamed"), CancellationToken.None);

        Assert.False(result);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenCityExists_RenamesAndSaves()
    {
        var city = ClassicCityTestSupport.CreateCity("Alpha City");
        city.ClearDomainEvents();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new RenameCityCommandHandler(cityRepository, unitOfWork);

        var result = await handler.Handle(new RenameCityCommand(city.Id.Value, "Neo City"), CancellationToken.None);

        Assert.True(result);
        Assert.Equal("Neo City", city.Name.Value);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}

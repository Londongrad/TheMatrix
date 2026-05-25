using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RenameCity
{
    public sealed class RenameCityCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var handler = new RenameCityCommandHandler(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork);

            bool result = await handler.Handle(
                request: new RenameCityCommand(
                    CityId: Guid.NewGuid(),
                    Name: "Renamed"),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handle_WhenCityExists_RenamesAndSaves()
        {
            City city = ClassicCityTestSupport.CreateCity();
            city.ClearDomainEvents();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var handler = new RenameCityCommandHandler(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork);

            bool result = await handler.Handle(
                request: new RenameCityCommand(
                    CityId: city.Id.Value,
                    Name: "Neo City"),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: "Neo City",
                actual: city.Name.Value);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}

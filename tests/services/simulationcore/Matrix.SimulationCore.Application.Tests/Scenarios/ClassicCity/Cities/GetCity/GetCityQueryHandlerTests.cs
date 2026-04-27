using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetCity;

public sealed class GetCityQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsNull()
    {
        Guid cityId = Guid.NewGuid();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var handler = new GetCityQueryHandler(cityRepository);

        var result = await handler.Handle(new GetCityQuery(cityId), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(cityId, cityRepository.RequestedCityId!.Value.Value);
    }

    [Fact]
    public async Task Handle_WhenCityExists_ReturnsMappedDto()
    {
        var city = ClassicCityTestSupport.CreateCity();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var handler = new GetCityQueryHandler(cityRepository);

        var result = await handler.Handle(new GetCityQuery(city.Id.Value), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(city.Id.Value, result!.CityId);
        Assert.Equal(city.Id.Value, result.SimulationId);
        Assert.Equal("Alpha City", result.Name);
        Assert.Equal("ClassicCity", result.SimulationKind);
        Assert.Equal(city.Status.ToString(), result.Status);
        Assert.Equal(city.Environment.UtcOffset.TotalMinutes, result.UtcOffsetMinutes);
        Assert.Equal(city.GenerationSeed.Value, result.GenerationSeed);
        Assert.Equal(city.CreatedAtUtc, result.CreatedAtUtc);
        Assert.False(result.IsArchived);
    }
}

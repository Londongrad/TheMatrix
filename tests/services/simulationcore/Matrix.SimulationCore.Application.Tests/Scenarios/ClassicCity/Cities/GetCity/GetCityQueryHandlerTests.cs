using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetCity;

public sealed class GetCityQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsNull()
    {
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var handler = new GetCityQueryHandler(cityRepository);

        var result = await handler.Handle(new GetCityQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenCityExists_ReturnsMappedDto()
    {
        var city = ClassicCityTestSupport.CreateCity("Neo Tokyo");
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var handler = new GetCityQueryHandler(cityRepository);

        var result = await handler.Handle(new GetCityQuery(city.Id.Value), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(city.Id.Value, result.CityId);
        Assert.Equal(city.Id.Value, result.SimulationId);
        Assert.Equal(city.Name.Value, result.Name);
        Assert.Equal(city.SimulationKind.ToString(), result.SimulationKind);
        Assert.Equal(city.Status.ToString(), result.Status);
        Assert.Equal(city.Environment.ClimateZone.ToString(), result.ClimateZone);
        Assert.Equal(city.Environment.Hemisphere.ToString(), result.Hemisphere);
        Assert.Equal(city.Environment.UtcOffset.TotalMinutes, result.UtcOffsetMinutes);
        Assert.Equal(city.GenerationProfile.PlannedPeopleCount, result.PlannedPeopleCount);
        Assert.Equal(city.PopulationBootstrapOperationId, result.PopulationBootstrapOperationId);
        Assert.Equal(city.EconomyBootstrapOperationId, result.EconomyBootstrapOperationId);
        Assert.Equal(city.CreatedAtUtc, result.CreatedAtUtc);
        Assert.False(result.IsArchived);
    }
}

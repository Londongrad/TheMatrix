using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;

public sealed class GetCityResidentsPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedPageAndPreservesRequestedCityAndPagination()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            PageByCityResult =
            (
                Items:
                [
                    CreatePerson(
                        personId: Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"),
                        firstName: "Neo",
                        lastName: "Anderson",
                        birthDate: new DateOnly(2030, 5, 4),
                        currentDate: new DateOnly(2048, 5, 4)),
                    CreatePerson(
                        personId: Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"),
                        firstName: "Trinity",
                        lastName: "Moss",
                        birthDate: new DateOnly(2029, 5, 4),
                        currentDate: new DateOnly(2048, 5, 4))
                ],
                TotalCount: 18
            )
        };
        var pagination = new Pagination(pageNumber: 3, pageSize: 2);
        var handler = new GetCityResidentsPageQueryHandler(personReadRepository);

        var result = await handler.Handle(
            new GetCityResidentsPageQuery(
                CityId: cityId,
                CurrentDate: new DateOnly(2048, 5, 4),
                Pagination: pagination),
            CancellationToken.None);

        Assert.Equal(CityId.From(cityId), personReadRepository.RequestedCityId);
        Assert.Equal(pagination, personReadRepository.RequestedPagination);
        Assert.Equal(18, result.TotalCount);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        PersonDto first = Assert.IsType<PersonDto>(result.Items.First());
        Assert.Equal("Anderson Neo", first.FullName);
        Assert.Equal(18, first.Age);
        Assert.Equal("Alive", first.LifeStatus);
    }
}

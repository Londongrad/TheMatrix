using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Contracts.Models;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.UseCases.Population.GetCitizenPage;

public sealed class GetCitizensPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedPagedResultAndPreservesRequestedPagination()
    {
        var personReadRepository = new FakePersonReadRepository
        {
            PageResult =
            (
                Items:
                [
                    CreatePerson(
                        personId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001"),
                        firstName: "Neo",
                        lastName: "Anderson",
                        birthDate: new DateOnly(2030, 5, 3),
                        currentDate: new DateOnly(2048, 5, 3)),
                    CreatePerson(
                        personId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000002"),
                        firstName: "Trinity",
                        lastName: "Moss",
                        birthDate: new DateOnly(2028, 5, 3),
                        currentDate: new DateOnly(2048, 5, 3))
                ],
                TotalCount: 12
            )
        };
        var pagination = new Pagination(pageNumber: 2, pageSize: 2);
        var handler = new GetCitizensPageQueryHandler(
            personReadRepository: personReadRepository,
            timeProvider: new FakeTimeProvider(UtcNow));

        var result = await handler.Handle(new GetCitizensPageQuery(pagination), CancellationToken.None);

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(pagination, personReadRepository.RequestedPagination);
        PersonDto first = Assert.IsType<PersonDto>(result.Items.First());
        Assert.Equal("Anderson Neo", first.FullName);
        Assert.Equal(18, first.Age);
        Assert.Equal("Alive", first.LifeStatus);
    }
}

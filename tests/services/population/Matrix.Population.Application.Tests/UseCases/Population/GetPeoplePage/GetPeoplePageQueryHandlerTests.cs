using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetPeoplePage;
using Matrix.Population.Contracts.Models;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.UseCases.Population.GetPeoplePage;

public sealed class GetPeoplePageQueryHandlerTests
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
                        birthDate: new DateOnly(
                            year: 2030,
                            month: 5,
                            day: 3),
                        currentDate: new DateOnly(
                            year: 2048,
                            month: 5,
                            day: 3)),
                    CreatePerson(
                        personId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000002"),
                        firstName: "Trinity",
                        lastName: "Moss",
                        birthDate: new DateOnly(
                            year: 2028,
                            month: 5,
                            day: 3),
                        currentDate: new DateOnly(
                            year: 2048,
                            month: 5,
                            day: 3))
                ],
                TotalCount: 12
            )
        };
        var pagination = new Pagination(
            pageNumber: 2,
            pageSize: 2);
        var handler = new GetPeoplePageQueryHandler(
            personReadRepository: personReadRepository,
            timeProvider: new FakeTimeProvider(UtcNow));

        PagedResult<PersonDto> result = await handler.Handle(
            request: new GetPeoplePageQuery(pagination),
            cancellationToken: CancellationToken.None);

        Assert.Equal(
            expected: 12,
            actual: result.TotalCount);
        Assert.Equal(
            expected: 2,
            actual: result.PageNumber);
        Assert.Equal(
            expected: 2,
            actual: result.PageSize);
        Assert.Equal(
            expected: pagination,
            actual: personReadRepository.RequestedPagination);
        PersonDto first = Assert.IsType<PersonDto>(result.Items.First());
        Assert.Equal(
            expected: "Anderson Neo",
            actual: first.FullName);
        Assert.Equal(
            expected: 18,
            actual: first.Age);
        Assert.Equal(
            expected: "Alive",
            actual: first.LifeStatus);
    }
}

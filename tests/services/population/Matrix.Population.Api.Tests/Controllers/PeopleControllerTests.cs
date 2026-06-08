using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Api.Controllers;
using Matrix.Population.Application.UseCases.Population.GetPeoplePage;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Controllers;

public sealed class PeopleControllerTests
{
    [Fact]
    public async Task GetPeoplePage_ForwardsPaginationAndReturnsPage()
    {
        var personId = Guid.Parse("76fefad3-fb16-437a-bd7e-63bca5ca4a8e");
        var sender = new FakeSender();
        sender.Handle<GetPeoplePageQuery, PagedResult<PersonDto>>(query =>
        {
            Assert.Equal(
                expected: 3,
                actual: query.Pagination.PageNumber);
            Assert.Equal(
                expected: 15,
                actual: query.Pagination.PageSize);

            return new PagedResult<PersonDto>(
                items:
                [
                    CreatePersonDto(
                        id: personId,
                        fullName: "Neo")
                ],
                totalCount: 1,
                pageNumber: 3,
                pageSize: 15);
        });
        var controller = new PeopleController(sender);

        ActionResult<PagedResult<PersonDto>> actionResult = await controller.GetPeoplePage(
            pageNumber: 3,
            pageSize: 15,
            cancellationToken: CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        PagedResult<PersonDto> page = Assert.IsType<PagedResult<PersonDto>>(ok.Value);
        Assert.Equal(
            expected: 3,
            actual: page.PageNumber);
        Assert.Equal(
            expected: "Neo",
            actual: Assert.Single(page.Items).FullName);
    }
}

using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Api.Controllers.Scenarios.ClassicCity;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class PopulationReadControllerTests
{
    [Fact]
    public async Task SummaryDashboardAndDistrictPressure_ReturnMissingOrOk()
    {
        Guid cityId = Guid.Parse("25d361a8-1eff-4ac7-90a6-3ae5d4cdfdd7");
        var sender = new FakeSender();
        sender.Handle<GetCityPopulationSummaryQuery, CityPopulationSummaryDto?>(_ => null);
        sender.Handle<GetCityDashboardQuery, CityPopulationDashboardDto?>(_ => CreateDashboardDto(cityId));
        sender.Handle<GetCityDistrictPressureQuery, CityPopulationDistrictPressureDto?>(_ => CreateDistrictPressureDto(cityId));
        var controller = new PopulationController(sender);

        ActionResult<CityPopulationSummaryDto> summaryResult = await controller.GetCitySummary(cityId, CancellationToken.None);
        ActionResult<CityPopulationDashboardDto> dashboardResult = await controller.GetCityDashboard(cityId, CancellationToken.None);
        ActionResult<CityPopulationDistrictPressureDto> pressureResult = await controller.GetCityDistrictPressure(cityId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(summaryResult.Result);

        OkObjectResult dashboardOk = Assert.IsType<OkObjectResult>(dashboardResult.Result);
        CityPopulationDashboardDto dashboard = Assert.IsType<CityPopulationDashboardDto>(dashboardOk.Value);
        Assert.Equal(cityId, dashboard.CityId);

        OkObjectResult pressureOk = Assert.IsType<OkObjectResult>(pressureResult.Result);
        CityPopulationDistrictPressureDto pressure = Assert.IsType<CityPopulationDistrictPressureDto>(pressureOk.Value);
        Assert.Equal(cityId, pressure.CityId);
        Assert.Single(pressure.Districts);
    }

    [Fact]
    public async Task ResidentAndCatalogQueries_ForwardInputsAndReturnOk()
    {
        Guid cityId = Guid.Parse("22c7bcc1-35cd-4f1b-8df2-7fc99292788a");
        Guid personId = Guid.Parse("76fefad3-fb16-437a-bd7e-63bca5ca4a8e");
        DateOnly currentDate = new(2048, 6, 1);
        var sender = new FakeSender();
        sender.Handle<GetCityResidentsPageQuery, PagedResult<PersonDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            Assert.Equal(currentDate, query.CurrentDate);
            Assert.Equal(2, query.Pagination.PageNumber);
            Assert.Equal(25, query.Pagination.PageSize);

            return new PagedResult<PersonDto>(
                items: [CreatePersonDto(personId, "Neo")],
                totalCount: 1,
                pageNumber: 2,
                pageSize: 25);
        });
        sender.Handle<GetCityResidentDetailsQuery, CityResidentDetailsDto>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            Assert.Equal(personId, query.PersonId);
            Assert.Equal(currentDate, query.CurrentDate);
            return CreateResidentDetailsDto(personId, "Neo");
        });
        sender.Handle<GetCityEmploymentCatalogQuery, CityEmploymentCatalogDto>(_ => CreateEmploymentCatalogDto());
        sender.Handle<GetCityEducationCatalogQuery, CityEducationCatalogDto>(_ => CreateEducationCatalogDto());
        sender.Handle<GetCitizensPageQuery, PagedResult<PersonDto>>(query =>
        {
            Assert.Equal(3, query.Pagination.PageNumber);
            Assert.Equal(15, query.Pagination.PageSize);

            return new PagedResult<PersonDto>(
                items: [CreatePersonDto(personId, "Neo")],
                totalCount: 1,
                pageNumber: 3,
                pageSize: 15);
        });
        var controller = new PopulationController(sender);

        ActionResult<PagedResult<PersonDto>> residentsResult = await controller.GetCityResidentsPage(
            cityId: cityId,
            currentDate: currentDate,
            pageNumber: 2,
            pageSize: 25,
            cancellationToken: CancellationToken.None);
        ActionResult<CityResidentDetailsDto> residentDetailsResult = await controller.GetCityResidentDetails(
            cityId: cityId,
            personId: personId,
            currentDate: currentDate,
            cancellationToken: CancellationToken.None);
        ActionResult<CityEmploymentCatalogDto> employmentCatalogResult =
            await controller.GetCityEmploymentCatalog(cityId, CancellationToken.None);
        ActionResult<CityEducationCatalogDto> educationCatalogResult =
            await controller.GetCityEducationCatalog(cityId, CancellationToken.None);
        ActionResult<PagedResult<PersonDto>> citizensResult = await controller.GetCitizensPage(
            pageNumber: 3,
            pageSize: 15,
            cancellationToken: CancellationToken.None);

        OkObjectResult residentsOk = Assert.IsType<OkObjectResult>(residentsResult.Result);
        PagedResult<PersonDto> residents = Assert.IsType<PagedResult<PersonDto>>(residentsOk.Value);
        Assert.Equal(2, residents.PageNumber);
        Assert.Equal("Neo", Assert.Single(residents.Items).FullName);

        OkObjectResult residentDetailsOk = Assert.IsType<OkObjectResult>(residentDetailsResult.Result);
        CityResidentDetailsDto residentDetails = Assert.IsType<CityResidentDetailsDto>(residentDetailsOk.Value);
        Assert.Equal(personId, residentDetails.Id);

        OkObjectResult employmentOk = Assert.IsType<OkObjectResult>(employmentCatalogResult.Result);
        CityEmploymentCatalogDto employmentCatalog = Assert.IsType<CityEmploymentCatalogDto>(employmentOk.Value);
        Assert.Equal(["Operator", "Medic"], employmentCatalog.JobTitles);

        OkObjectResult educationOk = Assert.IsType<OkObjectResult>(educationCatalogResult.Result);
        CityEducationCatalogDto educationCatalog = Assert.IsType<CityEducationCatalogDto>(educationOk.Value);
        Assert.Single(educationCatalog.CurrentInstitutions);

        OkObjectResult citizensOk = Assert.IsType<OkObjectResult>(citizensResult.Result);
        PagedResult<PersonDto> citizens = Assert.IsType<PagedResult<PersonDto>>(citizensOk.Value);
        Assert.Equal(3, citizens.PageNumber);
    }
}

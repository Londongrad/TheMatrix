using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Api.Controllers.Scenarios.ClassicCity;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class PopulationReadControllerTests
    {
        [Fact]
        public async Task SummaryDashboardAndDistrictPressure_ReturnMissingOrOk()
        {
            var cityId = Guid.Parse("25d361a8-1eff-4ac7-90a6-3ae5d4cdfdd7");
            var sender = new FakeSender();
            sender.Handle<GetCityPopulationSummaryQuery, CityPopulationSummaryDto?>(_ => null);
            sender.Handle<GetCityDashboardQuery, CityPopulationDashboardDto?>(_ => CreateDashboardDto(cityId));
            sender.Handle<GetCityDistrictPressureQuery, CityPopulationDistrictPressureDto?>(_
                => CreateDistrictPressureDto(cityId));
            var controller = new ClassicCityPopulationStateController(sender);

            ActionResult<CityPopulationSummaryDto> summaryResult = await controller.GetCitySummary(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            ActionResult<CityPopulationDashboardDto> dashboardResult = await controller.GetCityDashboard(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            ActionResult<CityPopulationDistrictPressureDto> pressureResult =
                await controller.GetCityDistrictPressure(
                    cityId: cityId,
                    cancellationToken: CancellationToken.None);

            Assert.IsType<NotFoundResult>(summaryResult.Result);

            OkObjectResult dashboardOk = Assert.IsType<OkObjectResult>(dashboardResult.Result);
            CityPopulationDashboardDto dashboard = Assert.IsType<CityPopulationDashboardDto>(dashboardOk.Value);
            Assert.Equal(
                expected: cityId,
                actual: dashboard.CityId);

            OkObjectResult pressureOk = Assert.IsType<OkObjectResult>(pressureResult.Result);
            CityPopulationDistrictPressureDto pressure =
                Assert.IsType<CityPopulationDistrictPressureDto>(pressureOk.Value);
            Assert.Equal(
                expected: cityId,
                actual: pressure.CityId);
            Assert.Single(pressure.Districts);
        }

        [Fact]
        public async Task ResidentAndCatalogQueries_ForwardInputsAndReturnOk()
        {
            var cityId = Guid.Parse("22c7bcc1-35cd-4f1b-8df2-7fc99292788a");
            var personId = Guid.Parse("76fefad3-fb16-437a-bd7e-63bca5ca4a8e");
            DateOnly currentDate = new(
                year: 2048,
                month: 6,
                day: 1);
            var sender = new FakeSender();
            sender.Handle<GetCityResidentsPageQuery, PagedResult<PersonDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                Assert.Equal(
                    expected: currentDate,
                    actual: query.CurrentDate);
                Assert.Equal(
                    expected: 2,
                    actual: query.Pagination.PageNumber);
                Assert.Equal(
                    expected: 25,
                    actual: query.Pagination.PageSize);

                return new PagedResult<PersonDto>(
                    items:
                    [
                        CreatePersonDto(
                            id: personId,
                            fullName: "Neo")
                    ],
                    totalCount: 1,
                    pageNumber: 2,
                    pageSize: 25);
            });
            sender.Handle<GetCityResidentDetailsQuery, CityResidentDetailsDto>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                Assert.Equal(
                    expected: personId,
                    actual: query.PersonId);
                Assert.Equal(
                    expected: currentDate,
                    actual: query.CurrentDate);
                return CreateResidentDetailsDto(
                    id: personId,
                    fullName: "Neo");
            });
            sender.Handle<GetCityEmploymentCatalogQuery, CityEmploymentCatalogDto>(_ => CreateEmploymentCatalogDto());
            sender.Handle<GetCityEducationCatalogQuery, CityEducationCatalogDto>(_ => CreateEducationCatalogDto());
            var controller = new PopulationController(sender);
            var educationController = new ClassicCityEducationController(sender);
            var employmentController = new ClassicCityEmploymentController(sender);
            var residentsController = new ClassicCityResidentsController(sender);

            ActionResult<PagedResult<PersonDto>> residentsResult = await residentsController.GetCityResidentsPage(
                cityId: cityId,
                currentDate: currentDate,
                pageNumber: 2,
                pageSize: 25,
                cancellationToken: CancellationToken.None);
            ActionResult<CityResidentDetailsDto> residentDetailsResult = await residentsController.GetCityResidentDetails(
                cityId: cityId,
                personId: personId,
                currentDate: currentDate,
                cancellationToken: CancellationToken.None);
            ActionResult<CityEmploymentCatalogDto> employmentCatalogResult =
                await employmentController.GetCityEmploymentCatalog(
                    cityId: cityId,
                    cancellationToken: CancellationToken.None);
            ActionResult<CityEducationCatalogDto> educationCatalogResult =
                await educationController.GetCityEducationCatalog(
                    cityId: cityId,
                    cancellationToken: CancellationToken.None);
            OkObjectResult residentsOk = Assert.IsType<OkObjectResult>(residentsResult.Result);
            PagedResult<PersonDto> residents = Assert.IsType<PagedResult<PersonDto>>(residentsOk.Value);
            Assert.Equal(
                expected: 2,
                actual: residents.PageNumber);
            Assert.Equal(
                expected: "Neo",
                actual: Assert.Single(residents.Items)
                   .FullName);

            OkObjectResult residentDetailsOk = Assert.IsType<OkObjectResult>(residentDetailsResult.Result);
            CityResidentDetailsDto residentDetails = Assert.IsType<CityResidentDetailsDto>(residentDetailsOk.Value);
            Assert.Equal(
                expected: personId,
                actual: residentDetails.Id);

            OkObjectResult employmentOk = Assert.IsType<OkObjectResult>(employmentCatalogResult.Result);
            CityEmploymentCatalogDto employmentCatalog = Assert.IsType<CityEmploymentCatalogDto>(employmentOk.Value);
            Assert.Equal(
                expected:
                [
                    "Operator",
                    "Medic"
                ],
                actual: employmentCatalog.JobTitles);

            OkObjectResult educationOk = Assert.IsType<OkObjectResult>(educationCatalogResult.Result);
            CityEducationCatalogDto educationCatalog = Assert.IsType<CityEducationCatalogDto>(educationOk.Value);
            Assert.Single(educationCatalog.CurrentInstitutions);

        }
    }
}

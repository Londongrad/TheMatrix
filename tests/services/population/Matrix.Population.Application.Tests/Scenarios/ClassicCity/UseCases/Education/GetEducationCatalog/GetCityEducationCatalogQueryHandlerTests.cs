using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog
{
    public sealed class GetCityEducationCatalogQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedInstitutionsForCity()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                EducationInstitutions =
                [
                    new CityEducationInstitutionSnapshot(
                        InstitutionId: EducationInstitutionId.From(Guid.Parse("11111111-2222-3333-4444-555555555555")),
                        InstitutionAnchorId: CityAnchorId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
                        EducationLevel: EducationLevel.Higher,
                        ResidentCount: 12),
                    new CityEducationInstitutionSnapshot(
                        InstitutionId: EducationInstitutionId.From(Guid.Parse("66666666-7777-8888-9999-000000000000")),
                        InstitutionAnchorId: null,
                        EducationLevel: EducationLevel.Primary,
                        ResidentCount: 45)
                ]
            };
            var handler = new GetCityEducationCatalogQueryHandler(personReadRepository);

            CityEducationCatalogDto result = await handler.Handle(
                request: new GetCityEducationCatalogQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: CityId.From(cityId),
                actual: personReadRepository.RequestedCityId);
            Assert.Equal(
                expected: 2,
                actual: result.CurrentInstitutions.Count);
            Assert.Equal(
                expected: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                actual: result.CurrentInstitutions[0].InstitutionId);
            Assert.Equal(
                expected: Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                actual: result.CurrentInstitutions[0].InstitutionAnchorId);
            Assert.Equal(
                expected: "Higher",
                actual: result.CurrentInstitutions[0].EducationLevel);
            Assert.Equal(
                expected: 12,
                actual: result.CurrentInstitutions[0].ResidentCount);
            Assert.Equal(
                expected: "Primary",
                actual: result.CurrentInstitutions[1].EducationLevel);
            Assert.Null(result.CurrentInstitutions[1].InstitutionAnchorId);
        }
    }
}

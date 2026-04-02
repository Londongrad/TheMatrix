using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog
{
    public sealed class GetCityEducationCatalogQueryHandler(
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository)
        : IRequestHandler<GetCityEducationCatalogQuery, CityEducationCatalogDto>
    {
        public async Task<CityEducationCatalogDto> Handle(
            GetCityEducationCatalogQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityEducationInstitutionDto> institutions =
                (await cityPopulationPersonReadRepository.ListEducationInstitutionsAsync(
                    cityId: CityId.From(request.CityId),
                    cancellationToken: cancellationToken))
               .Select(x => new CityEducationInstitutionDto(
                    InstitutionId: x.InstitutionId.Value,
                    InstitutionAnchorId: x.InstitutionAnchorId?.Value,
                    EducationLevel: x.EducationLevel.ToString(),
                    ResidentCount: x.ResidentCount))
               .ToArray();

            return new CityEducationCatalogDto(CurrentInstitutions: institutions);
        }
    }
}

using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog
{
    public sealed class GetCityEmploymentCatalogQueryHandler(
        IPopulationGenerationContentCatalog contentCatalog,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository)
        : IRequestHandler<GetCityEmploymentCatalogQuery, CityEmploymentCatalogDto>
    {
        public Task<CityEmploymentCatalogDto> Handle(
            GetCityEmploymentCatalogQuery request,
            CancellationToken cancellationToken)
        {
            return HandleInternalAsync(
                request: request,
                cancellationToken: cancellationToken);
        }

        private async Task<CityEmploymentCatalogDto> HandleInternalAsync(
            GetCityEmploymentCatalogQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> titles = contentCatalog.Professions
               .Select(x => x.Title)
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(
                    keySelector: x => x,
                    comparer: StringComparer.OrdinalIgnoreCase)
               .ToArray();
            IReadOnlyList<CityEmploymentWorkplaceDto> workplaces =
                (await cityPopulationPersonReadRepository.ListEmploymentWorkplacesAsync(
                    cityId: CityId.From(request.CityId),
                    cancellationToken: cancellationToken))
               .Select(x => new CityEmploymentWorkplaceDto(
                    WorkplaceId: x.WorkplaceId.Value,
                    JobTitle: x.JobTitle,
                    ResidentCount: x.ResidentCount))
               .ToArray();

            return new CityEmploymentCatalogDto(
                JobTitles: titles,
                CurrentWorkplaces: workplaces);
        }
    }
}

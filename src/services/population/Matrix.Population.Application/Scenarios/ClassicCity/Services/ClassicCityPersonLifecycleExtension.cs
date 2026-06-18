using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services
{
    public sealed class ClassicCityPersonLifecycleExtension(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationProgressionStateRepository cityPopulationProgressionStateRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        MarriageDomainService marriageDomainService,
        IPersonWriteRepository personWriteRepository) : IPersonLifecycleExtension
    {
        public async Task OnPersonDiedAsync(
            Person person,
            DateOnly fallbackCurrentDate,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            CityId? cityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: person.Id,
                cancellationToken: cancellationToken);
            if (cityId is null)
                return;

            DateOnly currentDate = (await cityPopulationProgressionStateRepository.GetByCityAsync(
                                       cityId: cityId.Value,
                                       cancellationToken: cancellationToken))?.LastProcessedDate ??
                                   fallbackCurrentDate;

            await RegisterWidowhoodAsync(
                deceased: person,
                cityId: cityId.Value,
                currentDate: currentDate,
                occurredAtUtc: occurredAtUtc,
                cancellationToken: cancellationToken);
            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: cityId.Value,
                currentDate: currentDate,
                cancellationToken: cancellationToken);
            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentDied(
                    cityId: cityId.Value.Value,
                    currentDate: currentDate,
                    resident: person,
                    source: CityPopulationActivitySource.Operator,
                    occurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
        }

        public async Task OnPersonResurrectedAsync(
            Person person,
            DateOnly fallbackCurrentDate,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            CityId? cityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: person.Id,
                cancellationToken: cancellationToken);
            if (cityId is null)
                return;

            DateOnly currentDate = (await cityPopulationProgressionStateRepository.GetByCityAsync(
                                       cityId: cityId.Value,
                                       cancellationToken: cancellationToken))?.LastProcessedDate ??
                                   fallbackCurrentDate;
            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: cityId.Value,
                currentDate: currentDate,
                cancellationToken: cancellationToken);
            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentResurrected(
                    cityId: cityId.Value.Value,
                    currentDate: currentDate,
                    resident: person,
                    source: CityPopulationActivitySource.Operator,
                    occurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
        }

        private async Task RegisterWidowhoodAsync(
            Person deceased,
            CityId cityId,
            DateOnly currentDate,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            if (deceased.SpouseId is null)
                return;

            Person? spouse = await personReadRepository.FindByIdAsync(
                id: deceased.SpouseId.Value,
                cancellationToken: cancellationToken);
            if (spouse is null)
                return;

            CityId? spouseCityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: spouse.Id,
                cancellationToken: cancellationToken);
            if (spouseCityId is null || spouseCityId.Value != cityId)
                return;

            bool spouseBecameWidowed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                deceased: deceased,
                spouse: spouse,
                marriageDomainService: marriageDomainService);
            if (!spouseBecameWidowed)
                return;

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentBecameWidowed(
                    cityId: cityId.Value,
                    currentDate: currentDate,
                    resident: spouse,
                    deceasedName: deceased.Name.ToString(),
                    source: CityPopulationActivitySource.Operator,
                    occurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
            await personWriteRepository.UpdateAsync(
                person: spouse,
                cancellationToken: cancellationToken);
        }
    }
}

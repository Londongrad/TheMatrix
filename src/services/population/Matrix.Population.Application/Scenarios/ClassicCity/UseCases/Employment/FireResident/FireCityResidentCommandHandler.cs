using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident
{
    public sealed class FireCityResidentCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IPersonWriteRepository personWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<FireCityResidentCommand, CityEmploymentOperationResultDto>
    {
        public async Task<CityEmploymentOperationResultDto> Handle(
            FireCityResidentCommand request,
            CancellationToken cancellationToken)
        {
            Person resident = await CityEmploymentOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.ResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            CityEmploymentOperationSupport.EnsureResidentCanBeFired(resident);
            string? previousJobTitle = resident.Employment.Job?.Title;
            resident.Fire(request.CurrentDate);

            await personWriteRepository.UpdateAsync(
                person: resident,
                cancellationToken: cancellationToken);

            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentFired(
                    cityId: request.CityId,
                    currentDate: request.CurrentDate,
                    resident: resident,
                    previousJobTitle: previousJobTitle,
                    source: CityPopulationActivitySource.Operator),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CityEmploymentOperationSupport.CreateResult(
                action: "ResidentFired",
                recordedAtUtc: DateTimeOffset.UtcNow,
                currentDate: request.CurrentDate,
                resident: resident);
        }
    }
}

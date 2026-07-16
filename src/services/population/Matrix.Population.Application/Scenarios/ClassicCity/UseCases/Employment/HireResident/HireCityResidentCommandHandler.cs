using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident
{
    public sealed class HireCityResidentCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IEducationParticipationProjectionRepository educationParticipationProjectionRepository,
        ICityEconomySettlementOutboxWriter cityEconomySettlementOutboxWriter,
        IPersonWriteRepository personWriteRepository,
        CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<HireCityResidentCommand, CityEmploymentOperationResultDto>
    {
        public async Task<CityEmploymentOperationResultDto> Handle(
            HireCityResidentCommand request,
            CancellationToken cancellationToken)
        {
            Person resident = await CityEmploymentOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.ResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            CityEmploymentWorkplaceSnapshot? workplace = request.WorkplaceId.HasValue
                ? await cityPopulationPersonReadRepository.FindEmploymentWorkplaceByIdAsync(
                    cityId: CityId.From(request.CityId),
                    workplaceId: WorkplaceId.From(request.WorkplaceId.Value),
                    cancellationToken: cancellationToken)
                : null;

            if (request.WorkplaceId.HasValue && workplace is null)
                throw ClassicCityApplicationErrorsFactory.EmploymentWorkplaceNotFound(
                    workplaceId: request.WorkplaceId.Value,
                    cityId: request.CityId);

            CityResidentHousingSnapshot? housing =
                await cityPopulationPersonReadRepository.FindHousingSnapshotByPersonIdAsync(
                    cityId: CityId.From(request.CityId),
                    personId: resident.Id,
                    cancellationToken: cancellationToken);

            Job job = await CityEmploymentOperationSupport.CreateJobAsync(
                cityId: request.CityId,
                resident: resident,
                jobTitle: request.JobTitle,
                workplace: workplace,
                housing: housing,
                cityPopulationAnchorCatalogRepository: cityPopulationAnchorCatalogRepository,
                anchorSelectionPolicy: anchorSelectionPolicy,
                cancellationToken: cancellationToken);
            resident.AssignJob(
                currentDate: request.CurrentDate,
                job: job);
            DateTimeOffset recordedAtUtc = timeProvider.GetUtcNow();

            await personWriteRepository.UpdateAsync(
                person: resident,
                cancellationToken: cancellationToken);

            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentHired(
                    cityId: request.CityId,
                    currentDate: request.CurrentDate,
                    resident: resident,
                    source: CityPopulationActivitySource.Operator,
                    occurredAtUtc: recordedAtUtc),
                cancellationToken: cancellationToken);

            string workplaceSyncCorrelationId =
                $"classic-city:{request.CityId:N}:resident-hire:{resident.Id.Value:N}:workplaces";
            foreach (ClassicCityWorkplaceBusinessSyncBatchV1 batch in
                     ClassicCityWorkplaceBusinessSyncBatchFactory.Build(
                         cityId: request.CityId,
                         persons: [resident],
                         correlationId: workplaceSyncCorrelationId,
                         occurredAtUtc: recordedAtUtc,
                         batchSize: 1))
                await cityEconomySettlementOutboxWriter.AddClassicCityWorkplaceBusinessSyncBatchAsync(
                    batch: batch,
                    cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await CityEmploymentOperationSupport.CreateResultAsync(
                action: "EmploymentAssigned",
                recordedAtUtc: recordedAtUtc,
                cityId: request.CityId,
                currentDate: request.CurrentDate,
                resident: resident,
                educationParticipationProjectionRepository: educationParticipationProjectionRepository,
                cancellationToken: cancellationToken);
        }
    }
}

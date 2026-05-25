using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.EnrollResident
{
    public sealed class EnrollCityResidentCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
        IPersonWriteRepository personWriteRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<EnrollCityResidentCommand, CityEducationOperationResultDto>
    {
        public async Task<CityEducationOperationResultDto> Handle(
            EnrollCityResidentCommand request,
            CancellationToken cancellationToken)
        {
            Person resident = await CityEducationOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.ResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            CityEducationOperationSupport.EnsureResidentCanEnroll(
                resident: resident,
                currentDate: request.CurrentDate);

            CityEducationInstitutionSnapshot? institution = await CityEducationOperationSupport.ResolveInstitutionAsync(
                cityId: request.CityId,
                institutionId: request.InstitutionId,
                expectedEducationLevel: resident.EducationLevel,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);
            CityResidentHousingSnapshot? housing =
                await cityPopulationPersonReadRepository.FindHousingSnapshotByPersonIdAsync(
                    cityId: CityId.From(request.CityId),
                    personId: resident.Id,
                    cancellationToken: cancellationToken);
            CityEducationInstitutionBinding institutionBinding =
                await CityEducationOperationSupport.CreateInstitutionBindingAsync(
                    cityId: request.CityId,
                    resident: resident,
                    institution: institution,
                    housing: housing,
                    educationLevel: resident.EducationLevel,
                    cityPopulationAnchorCatalogRepository: cityPopulationAnchorCatalogRepository,
                    anchorSelectionPolicy: anchorSelectionPolicy,
                    cancellationToken: cancellationToken);

            resident.StartStudying(
                currentDate: request.CurrentDate,
                institutionId: institutionBinding.InstitutionId,
                institutionAnchorId: institutionBinding.InstitutionAnchorId);
            DateTimeOffset recordedAtUtc = timeProvider.GetUtcNow();

            await personWriteRepository.UpdateAsync(
                person: resident,
                cancellationToken: cancellationToken);

            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentEnrolled(
                    cityId: request.CityId,
                    currentDate: request.CurrentDate,
                    resident: resident,
                    source: CityPopulationActivitySource.Operator,
                    occurredAtUtc: recordedAtUtc),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CityEducationOperationSupport.CreateResult(
                action: "ResidentEnrolledInStudy",
                recordedAtUtc: recordedAtUtc,
                currentDate: request.CurrentDate,
                resident: resident);
        }
    }
}

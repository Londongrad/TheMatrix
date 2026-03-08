using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GraduateResident
{
    public sealed class GraduateCityResidentCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IPersonWriteRepository personWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<GraduateCityResidentCommand, CityEducationOperationResultDto>
    {
        public async Task<CityEducationOperationResultDto> Handle(
            GraduateCityResidentCommand request,
            CancellationToken cancellationToken)
        {
            Person resident = await CityEducationOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.ResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            EducationLevel targetEducationLevel = CityEducationOperationSupport.ParseTargetEducationLevel(
                request.TargetEducationLevel);

            CityEducationOperationSupport.EnsureResidentCanGraduate(
                resident: resident,
                targetEducationLevel: targetEducationLevel);

            CityEducationInstitutionSnapshot? institution = await CityEducationOperationSupport.ResolveInstitutionAsync(
                cityId: request.CityId,
                institutionId: request.InstitutionId,
                expectedEducationLevel: targetEducationLevel,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            resident.GraduateTo(
                newLevel: targetEducationLevel,
                institutionId: CityEducationOperationSupport.ResolveInstitutionId(institution));

            await personWriteRepository.UpdateAsync(
                person: resident,
                cancellationToken: cancellationToken);

            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentGraduated(
                    cityId: request.CityId,
                    currentDate: request.CurrentDate,
                    resident: resident,
                    source: Domain.Scenarios.ClassicCity.Enums.CityPopulationActivitySource.Operator),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CityEducationOperationSupport.CreateResult(
                action: "ResidentGraduated",
                recordedAtUtc: DateTimeOffset.UtcNow,
                currentDate: request.CurrentDate,
                resident: resident);
        }
    }
}

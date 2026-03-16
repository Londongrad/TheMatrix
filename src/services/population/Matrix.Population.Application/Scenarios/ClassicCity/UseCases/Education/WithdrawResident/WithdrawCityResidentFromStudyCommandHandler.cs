using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident
{
    public sealed class WithdrawCityResidentFromStudyCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IPersonWriteRepository personWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<WithdrawCityResidentFromStudyCommand, CityEducationOperationResultDto>
    {
        public async Task<CityEducationOperationResultDto> Handle(
            WithdrawCityResidentFromStudyCommand request,
            CancellationToken cancellationToken)
        {
            Person resident = await CityEducationOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.ResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            CityEducationOperationSupport.EnsureResidentCanWithdraw(resident);
            resident.StopStudying(request.CurrentDate);

            await personWriteRepository.UpdateAsync(
                person: resident,
                cancellationToken: cancellationToken);

            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentWithdrewFromStudy(
                    cityId: request.CityId,
                    currentDate: request.CurrentDate,
                    resident: resident,
                    source: CityPopulationActivitySource.Operator),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CityEducationOperationSupport.CreateResult(
                action: "ResidentWithdrawnFromStudy",
                recordedAtUtc: DateTimeOffset.UtcNow,
                currentDate: request.CurrentDate,
                resident: resident);
        }
    }
}

using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed record RunCityMunicipalOperatingCycleCommand(Guid CityId)
        : IRequest<RunCityMunicipalOperatingCycleResultDto>;
}

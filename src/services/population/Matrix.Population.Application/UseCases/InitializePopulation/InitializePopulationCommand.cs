using MediatR;

namespace Matrix.Population.Application.UseCases.InitializePopulation
{
    public sealed record InitializePopulationCommand(
        int PeopleCount,
        int? RandomSeed) : IRequest<Unit>;
}

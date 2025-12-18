using MediatR;

namespace Matrix.Population.Application.UseCases.Population.InitializePopulation
{
    public sealed record InitializePopulationCommand(
        int PeopleCount,
        int? RandomSeed) : IRequest;
}

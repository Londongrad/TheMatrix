using Matrix.Population.Application.DTOs;
using MediatR;

namespace Matrix.Population.Application.UseCases.InitializePopulation
{
    public sealed record InitializePopulationCommand(
        int PeopleCount,
        int? RandomSeed) : IRequest<IReadOnlyList<PersonDto>>;
}

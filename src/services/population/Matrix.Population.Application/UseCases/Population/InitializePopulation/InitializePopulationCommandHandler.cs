using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.InitializePopulation
{
    public sealed class InitializePopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        PopulationGenerator generator)
        : IRequestHandler<InitializePopulationCommand>
    {
        public async Task Handle(
            InitializePopulationCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await personWriteRepository.DeleteAllAsync(cancellationToken);

            // TODO: Получать дату извне
            IReadOnlyCollection<Domain.Entities.Person> persons = generator.Generate(
                peopleCount: request.PeopleCount,
                currentDate: DateOnly.FromDateTime(DateTime.UtcNow),
                randomSeed: request.RandomSeed);

            await personWriteRepository.AddRangeAsync(
                persons: persons,
                cancellationToken: cancellationToken);

            await personWriteRepository.SaveChangesAsync(cancellationToken);
        }
    }
}

using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.UseCases.InitializePopulation
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
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            await personWriteRepository.DeleteAllAsync(cancellationToken);

            // TODO: Получать дату извне
            var persons = generator.Generate(
                request.PeopleCount,
                DateOnly.FromDateTime(DateTime.UtcNow),
                request.RandomSeed);

            await personWriteRepository.AddRangeAsync(persons, cancellationToken);

            await personWriteRepository.SaveChangesAsync(cancellationToken);
        }
    }
}

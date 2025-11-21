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
        private readonly IPersonWriteRepository _personWriteRepository = personWriteRepository;
        private readonly PopulationGenerator _generator = generator;

        public async Task Handle(
                InitializePopulationCommand request,
                CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            await _personWriteRepository.DeleteAllAsync(cancellationToken);

            // TODO: Получать дату извне
            var persons = _generator.Generate(
                request.PeopleCount,
                DateOnly.FromDateTime(DateTime.UtcNow),
                request.RandomSeed);

            await _personWriteRepository.AddRangeAsync(persons, cancellationToken);
        }
    }
}

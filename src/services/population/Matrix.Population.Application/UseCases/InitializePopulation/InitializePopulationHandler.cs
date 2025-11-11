using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.UseCases.InitializePopulation
{
    public sealed class InitializePopulationHandler(
        IPersonWriteRepository personWriteRepository,
        PopulationGenerator generator)
        : IRequestHandler<InitializePopulationCommand, Unit>
    {
        private readonly IPersonWriteRepository _personWriteRepository = personWriteRepository;
        private readonly PopulationGenerator _generator = generator;

        public async Task<Unit> Handle(
            InitializePopulationCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Очистить текущую популяцию (если это "reset")
            await _personWriteRepository.DeleteAllAsync(cancellationToken);

            // 2. Сгенерировать людей
            var persons = _generator.Generate(
                request.PeopleCount,
                request.RandomSeed);

            // 3. Сохранить в БД
            await _personWriteRepository.AddRangeAsync(persons, cancellationToken);

            return Unit.Value;
        }
    }
}

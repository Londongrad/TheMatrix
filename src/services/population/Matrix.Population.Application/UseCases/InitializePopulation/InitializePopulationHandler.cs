using AutoMapper;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.DTOs;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.UseCases.InitializePopulation
{
    public sealed class InitializePopulationHandler(
        IPersonWriteRepository personWriteRepository,
        PopulationGenerator generator,
        IMapper mapper)
        : IRequestHandler<InitializePopulationCommand, IReadOnlyList<PersonDto>>
    {
        private readonly IPersonWriteRepository _personWriteRepository = personWriteRepository;
        private readonly PopulationGenerator _generator = generator;
        private readonly IMapper _mapper = mapper;

    public async Task<IReadOnlyList<PersonDto>> Handle(
            InitializePopulationCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Очистить текущую популяцию (если это "reset")
            //await _personWriteRepository.DeleteAllAsync(cancellationToken);

            // 2. Сгенерировать людей
            var persons = _generator.Generate(
                request.PeopleCount,
                DateOnly.FromDateTime(DateTime.UtcNow),
                request.RandomSeed);

            // 3. Сохранить в БД
            //await _personWriteRepository.AddRangeAsync(persons, cancellationToken);


            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var dtos = _mapper.Map<List<PersonDto>>(
                persons,
                opt => opt.Items["currentDate"] = currentDate
            );

            return dtos;
        }
    }
}

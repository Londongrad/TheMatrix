using AutoMapper;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.DTOs;
using MediatR;

namespace Matrix.Population.Application.UseCases.KillPerson
{
    public class KillPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository,
        IMapper mapper) 
        : IRequestHandler<KillPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(KillPersonCommand request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var person = await personReadRepository.GetByIdAsync(request.Id, cancellationToken) 
                ?? throw new KeyNotFoundException($"Person with Id {request.Id} not found.");

            person.Die();

            await personWriteRepository.UpdateAsync(person, cancellationToken);

            return mapper.Map<PersonDto>(person);
        }
    }
}

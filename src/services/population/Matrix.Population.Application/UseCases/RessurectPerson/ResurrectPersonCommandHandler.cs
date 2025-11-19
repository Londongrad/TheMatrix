using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.RessurectPerson
{
    public sealed class ResurrectPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository
        )
        : IRequestHandler<ResurrectPersonCommand, PersonDto>
    {
        private readonly IPersonWriteRepository _personWriteRepository = personWriteRepository;
        private readonly IPersonReadRepository _personReadRepository = personReadRepository;

        public async Task<PersonDto> Handle(
            ResurrectPersonCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var person = await _personReadRepository.GetByIdAsync(PersonId.From(request.Id), cancellationToken);

            // TODO: Брать дату извне
            person.Resurrect(DateOnly.FromDateTime(DateTime.UtcNow));

            await _personWriteRepository.UpdateAsync(person, cancellationToken);

            return person.ToDto();
        }
    }
}

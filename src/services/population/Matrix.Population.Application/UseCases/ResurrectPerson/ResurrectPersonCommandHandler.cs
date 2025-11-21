using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.ResurrectPerson
{
    public sealed class ResurrectPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository
        )
        : IRequestHandler<ResurrectPersonCommand, PersonDto>
    {

        public async Task<PersonDto> Handle(
            ResurrectPersonCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var person = await personReadRepository.GetByIdAsync(PersonId.From(request.Id), cancellationToken);

            // TODO: Брать дату извне
            person.Resurrect(DateOnly.FromDateTime(DateTime.UtcNow));

            await personWriteRepository.UpdateAsync(person, cancellationToken);
            await personWriteRepository.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
    }
}

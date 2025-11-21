using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.KillPerson
{
    public class KillPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository)
        : IRequestHandler<KillPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(KillPersonCommand request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var person = await personReadRepository.GetByIdAsync(PersonId.From(request.Id), cancellationToken);

            // TODO: Получать дату извне
            person.Die(DateOnly.FromDateTime(DateTime.UtcNow));

            await personWriteRepository.UpdateAsync(person, cancellationToken);
            await personWriteRepository.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
    }
}

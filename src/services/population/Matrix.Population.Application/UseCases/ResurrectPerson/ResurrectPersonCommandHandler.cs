using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.ResurrectPerson
{
    public sealed class ResurrectPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository)
        : IRequestHandler<ResurrectPersonCommand, PersonDto>
    {

        public async Task<PersonDto> Handle(
            ResurrectPersonCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var person = await personReadRepository.FindByIdAsync(PersonId.From(request.Id), cancellationToken)
                ?? throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            person.Resurrect();

            await personWriteRepository.UpdateAsync(person, cancellationToken);
            await personWriteRepository.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
    }
}

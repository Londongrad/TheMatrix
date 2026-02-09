using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public class KillPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<KillPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            KillPersonCommand request,
            CancellationToken cancellationToken = default)
        {
            request = GuardHelper.AgainstNull(
                value: request,
                errorFactory: ApplicationErrorsFactory.Required);

            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            person.Die(DateOnly.FromDateTime(DateTime.UtcNow));

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
    }
}

using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.ResurrectPerson
{
    public sealed class ResurrectPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ResurrectPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            ResurrectPersonCommand request,
            CancellationToken cancellationToken)
        {
            request = GuardHelper.AgainstNull(
                value: request,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: nameof(request));

            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            person.Resurrect();

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
    }
}

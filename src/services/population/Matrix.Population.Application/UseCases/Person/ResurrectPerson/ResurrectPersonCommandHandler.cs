using Matrix.BuildingBlocks.Application.Abstractions;
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
        IEnumerable<IPersonLifecycleExtension> lifecycleExtensions,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ResurrectPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            ResurrectPersonCommand request,
            CancellationToken cancellationToken)
        {
            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();
            var currentDate = DateOnly.FromDateTime(occurredAtUtc.UtcDateTime);
            person.Resurrect();

            foreach (IPersonLifecycleExtension extension in lifecycleExtensions)
                await extension.OnPersonResurrectedAsync(
                    person: person,
                    fallbackCurrentDate: currentDate,
                    occurredAtUtc: occurredAtUtc,
                    cancellationToken: cancellationToken);

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto(timeProvider);
        }
    }
}

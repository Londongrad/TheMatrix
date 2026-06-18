using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public sealed class KillPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository,
        IEnumerable<IPersonLifecycleExtension> lifecycleExtensions,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<KillPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            KillPersonCommand request,
            CancellationToken cancellationToken = default)
        {
            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();
            var currentDate = DateOnly.FromDateTime(occurredAtUtc.UtcDateTime);
            person.Die(currentDate);

            foreach (IPersonLifecycleExtension extension in lifecycleExtensions)
                await extension.OnPersonDiedAsync(
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

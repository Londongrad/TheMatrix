using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.UpdatePerson
{
    public class UpdatePersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository)
        : IRequestHandler<UpdatePersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request.Changes);

            var person = await personReadRepository.GetByIdAsync(PersonId.From(request.Id), cancellationToken);

            // TODO: получать дату извне
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            ApplyBasicInfo(person, request.Changes);
            ApplyEmployment(person, request.Changes, currentDate);
            ApplyHappiness(person, request.Changes);

            await personWriteRepository.UpdateAsync(person, cancellationToken);

            return person.ToDto();
        }

        private static void ApplyBasicInfo(Person person, UpdatePersonRequest changes)
        {
            if (!string.IsNullOrWhiteSpace(changes.FullName))
            {
                var name = PersonName.FromFullName(changes.FullName);
                person.ChangeName(name);
            }

            if (changes.MaritalStatus is not null)
            {
                person.ChangeMaritalStatus(changes.MaritalStatus.Value);
            }

            if (changes.EducationLevel is not null)
            {
                person.SetEducationLevel(changes.EducationLevel.Value);
            }
        }

        private static void ApplyEmployment(
            Person person,
            UpdatePersonRequest changes,
            DateOnly currentDate)
        {
            if (changes.EmploymentStatus is null && changes.JobTitle is null)
            {
                return;
            }

            if (changes.EmploymentStatus is EmploymentStatus.Employed && changes.JobTitle is not null)
            {
                // TODO: Заменить на реальный id работы
                var job = new Job(WorkplaceId.New(), changes.JobTitle!);
                person.SetEmploymentStatus(currentDate, changes.EmploymentStatus.Value, job);
                return;
            }
            person.SetEmploymentStatus(currentDate, changes.EmploymentStatus!.Value);
        }

        private static void ApplyHappiness(Person person, UpdatePersonRequest changes)
        {
            if (changes.Happiness is null)
                return;

            person.SetHappiness(changes.Happiness.Value);
        }
    }
}

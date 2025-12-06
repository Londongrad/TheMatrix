using Matrix.BuildingBlocks.Domain;
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

            Person person =
                await personReadRepository.FindByIdAsync(id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken)
                ?? throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            // TODO: получать дату извне
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            ApplyBasicInfo(person: person, changes: request.Changes);
            ApplyEmployment(person: person, changes: request.Changes, currentDate: currentDate);
            ApplyHappiness(person: person, changes: request.Changes);

            await personWriteRepository.UpdateAsync(person: person, cancellationToken: cancellationToken);
            await personWriteRepository.SaveChangesAsync(cancellationToken);
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
                MaritalStatus maritalStatus
                    = GuardHelper.AgainstInvalidStringToEnum<MaritalStatus>(value: changes.MaritalStatus,
                        propertyName: nameof(changes.MaritalStatus));
                person.ChangeMaritalStatus(maritalStatus);
            }

            if (changes.EducationLevel is not null)
            {
                EducationLevel educationLevel
                    = GuardHelper.AgainstInvalidStringToEnum<EducationLevel>(value: changes.EducationLevel,
                        propertyName: nameof(changes.EducationLevel));
                person.SetEducationLevel(educationLevel);
            }
        }

        private static void ApplyEmployment(
            Person person,
            UpdatePersonRequest changes,
            DateOnly currentDate)
        {
            if (changes.EmploymentStatus is null && changes.JobTitle is null) return;

            EmploymentStatus employmentStatus
                = GuardHelper.AgainstInvalidStringToEnum<EmploymentStatus>(value: changes.EmploymentStatus!,
                    propertyName: nameof(changes.EmploymentStatus));

            if (changes.EmploymentStatus is "Employed" && changes.JobTitle is not null)
            {
                // TODO: Заменить на реальный id работы
                var job = new Job(workplaceId: WorkplaceId.New(), title: changes.JobTitle!);
                person.SetEmploymentStatus(currentDate, employmentStatus, job);
                return;
            }

            person.SetEmploymentStatus(currentDate, employmentStatus);
        }

        private static void ApplyHappiness(Person person, UpdatePersonRequest changes)
        {
            if (changes.Happiness is null)
                return;

            person.SetHappiness(changes.Happiness.Value);
        }
    }
}

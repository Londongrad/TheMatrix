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

            var person = await personReadRepository.GetByIdAsync(PersonId.From(request.Id), cancellationToken);

            // TODO: получать дату извне
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            ApplyBasicInfo(person, request.Changes);
            ApplyEmployment(person, request.Changes, currentDate);
            ApplyHappiness(person, request.Changes);

            await personWriteRepository.UpdateAsync(person, cancellationToken);
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
                var maritalStatus
                    = GuardHelper.AgainstInvalidStringToEnum<MaritalStatus>(changes.MaritalStatus, nameof(changes.MaritalStatus));
                person.ChangeMaritalStatus(maritalStatus);
            }

            if (changes.EducationLevel is not null)
            {
                var educationLevel
                    = GuardHelper.AgainstInvalidStringToEnum<EducationLevel>(changes.EducationLevel, nameof(changes.EducationLevel));
                person.SetEducationLevel(educationLevel);
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

            var employmentStatus 
                = GuardHelper.AgainstInvalidStringToEnum<EmploymentStatus>(changes.EmploymentStatus!, nameof(changes.EmploymentStatus));

            if (changes.EmploymentStatus is "Employed" && changes.JobTitle is not null)
            {
                // TODO: Заменить на реальный id работы
                var job = new Job(WorkplaceId.New(), changes.JobTitle!);
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

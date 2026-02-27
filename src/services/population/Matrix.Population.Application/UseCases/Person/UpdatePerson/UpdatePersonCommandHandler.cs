using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.UpdatePerson
{
    public sealed class UpdatePersonCommandHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdatePersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            UpdatePersonCommand request,
            CancellationToken cancellationToken)
        {
            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                person.ChangeName(PersonName.FromFullName(request.FullName.Trim()));
            }

            DateOnly currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            ApplyAbsoluteMetric(request.Health, person.Health.Value, delta => person.ChangeHealth(delta, currentDate));
            ApplyAbsoluteMetric(request.Happiness, person.Happiness.Value, person.ChangeHappiness);
            ApplyAbsoluteMetric(request.Energy, person.Energy.Value, person.ChangeEnergy);
            ApplyAbsoluteMetric(request.Stress, person.Stress.Value, person.ChangeStress);
            ApplyAbsoluteMetric(request.SocialNeed, person.SocialNeed.Value, person.ChangeSocialNeed);

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
        private static void ApplyAbsoluteMetric(
            int? requestedValue,
            int currentValue,
            Action<int> applyDelta)
        {
            if (!requestedValue.HasValue)
            {
                return;
            }

            int delta = requestedValue.Value - currentValue;
            if (delta != 0)
            {
                applyDelta(delta);
            }
        }
    }
}

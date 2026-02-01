using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;
using MediatR;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        ICityPopulationProgressionStateRepository progressionStateRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<AdvanceCityPopulationCommand, AdvanceCityPopulationResult>
    {
        public async Task<AdvanceCityPopulationResult> Handle(
            AdvanceCityPopulationCommand request,
            CancellationToken cancellationToken)
        {
            if (request.FromSimTimeUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("FromSimTimeUtc must be UTC.", nameof(request.FromSimTimeUtc));

            if (request.ToSimTimeUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("ToSimTimeUtc must be UTC.", nameof(request.ToSimTimeUtc));

            if (request.TickId < 0)
                throw new ArgumentOutOfRangeException(nameof(request.TickId));

            var cityId = CityId.From(request.CityId);
            DateOnly fromDate = DateOnly.FromDateTime(request.FromSimTimeUtc.UtcDateTime);
            DateOnly toDate = DateOnly.FromDateTime(request.ToSimTimeUtc.UtcDateTime);

            if (toDate < fromDate)
                throw new ArgumentException("ToSimTimeUtc date cannot be earlier than FromSimTimeUtc date.");

            CityPopulationProgressionState? state = await progressionStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (state is not null)
            {
                if (request.TickId <= state.LastProcessedTickId)
                    return new AdvanceCityPopulationResult(
                        Status: AdvanceCityPopulationStatus.Duplicate,
                        AffectedPeopleCount: 0);

                if (toDate < state.LastProcessedDate)
                    return new AdvanceCityPopulationResult(
                        Status: AdvanceCityPopulationStatus.OutOfOrder,
                        AffectedPeopleCount: 0);
            }

            DateOnly previousDate = state?.LastProcessedDate ?? fromDate;
            int affectedPeopleCount = 0;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    if (state is null || toDate > previousDate)
                    {
                        IReadOnlyCollection<PersonEntity> persons = await personWriteRepository.ListByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                        foreach (PersonEntity person in persons)
                            if (ApplyTimeProgression(
                                    person: person,
                                    currentDate: toDate))
                                affectedPeopleCount++;
                    }

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (state is null)
                    {
                        CityPopulationProgressionState newState = CityPopulationProgressionState.Create(
                            cityId: cityId,
                            lastProcessedTickId: request.TickId,
                            lastProcessedDate: toDate,
                            updatedAtUtc: updatedAtUtc);

                        await progressionStateRepository.AddAsync(
                            state: newState,
                            cancellationToken: ct);
                    }
                    else
                    {
                        state.MarkProcessed(
                            tickId: request.TickId,
                            processedDate: toDate,
                            updatedAtUtc: updatedAtUtc);
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new AdvanceCityPopulationResult(
                Status: AdvanceCityPopulationStatus.Applied,
                AffectedPeopleCount: affectedPeopleCount);
        }

        private static bool ApplyTimeProgression(
            PersonEntity person,
            DateOnly currentDate)
        {
            if (!person.IsAlive)
                return false;

            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return false;

            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return false;

            person.Retire(currentDate);
            return true;
        }
    }
}

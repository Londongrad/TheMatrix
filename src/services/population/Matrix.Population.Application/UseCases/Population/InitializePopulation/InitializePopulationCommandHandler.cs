using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.InitializePopulation
{
    public sealed class InitializePopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        IHouseholdWriteRepository householdWriteRepository,
        CityPopulationBootstrapGenerator generator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<InitializePopulationCommand>
    {
        public async Task Handle(
            InitializePopulationCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    await personWriteRepository.DeleteAllAsync(ct);
                    await householdWriteRepository.DeleteAllAsync(ct);

                    PopulationBootstrapResult result = generator.GenerateStandalone(
                        peopleCount: request.PeopleCount,
                        currentDate: DateOnly.FromDateTime(DateTime.UtcNow),
                        createdAtUtc: DateTimeOffset.UtcNow,
                        randomSeed: request.RandomSeed);

                    await householdWriteRepository.AddRangeAsync(
                        households: result.Households,
                        cancellationToken: ct);

                    await personWriteRepository.AddRangeAsync(
                        persons: result.Persons,
                        cancellationToken: ct);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);
        }
    }
}

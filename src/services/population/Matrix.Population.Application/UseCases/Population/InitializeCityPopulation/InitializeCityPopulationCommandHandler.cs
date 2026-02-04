using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.UseCases.Population.Common;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.InitializeCityPopulation
{
    public sealed class InitializeCityPopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        IHouseholdWriteRepository householdWriteRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        CityPopulationBootstrapGenerator generator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<InitializeCityPopulationCommand, CityPopulationBootstrapSummaryDto>
    {
        public async Task<CityPopulationBootstrapSummaryDto> Handle(
            InitializeCityPopulationCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Environment);

            var cityId = CityId.From(request.CityId);
            IReadOnlyCollection<ResidentialBuildingResidence> residentialBuildings = request.ResidentialBuildings
               .Select(x => new ResidentialBuildingResidence(
                    residentialBuildingId: ResidentialBuildingId.From(x.ResidentialBuildingId),
                    districtId: DistrictId.From(x.DistrictId),
                    residentCapacity: x.ResidentCapacity))
               .ToArray();

            PopulationBootstrapResult result = generator.GenerateForCity(
                cityId: cityId,
                residentialBuildings: residentialBuildings,
                peopleCount: request.PeopleCount,
                currentDate: request.CurrentDate,
                createdAtUtc: DateTimeOffset.UtcNow,
                randomSeed: request.RandomSeed);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
                    CityPopulationEnvironment? environment = await cityPopulationEnvironmentRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (environment is null)
                    {
                        CityPopulationEnvironment newEnvironment = CityPopulationEnvironmentMapper.Create(
                            cityId: request.CityId,
                            input: request.Environment,
                            createdAtUtc: updatedAtUtc);

                        await cityPopulationEnvironmentRepository.AddAsync(
                            environment: newEnvironment,
                            cancellationToken: ct);
                    }
                    else
                    {
                        CityPopulationEnvironmentMapper.Sync(
                            environment: environment,
                            input: request.Environment,
                            updatedAtUtc: updatedAtUtc);
                    }

                    await householdWriteRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    await householdWriteRepository.AddRangeAsync(
                        households: result.Households,
                        cancellationToken: ct);

                    await personWriteRepository.AddRangeAsync(
                        persons: result.Persons,
                        cancellationToken: ct);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            int housedHouseholdCount = result.Households.Count(x => x.HousingStatus == HousingStatus.Housed);
            int homelessHouseholdCount = result.Households.Count - housedHouseholdCount;
            int housedPeopleCount = result.Households
               .Where(x => x.HousingStatus == HousingStatus.Housed)
               .Sum(x => x.Size.Value);
            int homelessPeopleCount = result.Persons.Count - housedPeopleCount;

            return new CityPopulationBootstrapSummaryDto(
                CityId: request.CityId,
                RequestedPeopleCount: request.PeopleCount,
                GeneratedPeopleCount: result.Persons.Count,
                HouseholdCount: result.Households.Count,
                HousedHouseholdCount: housedHouseholdCount,
                HomelessHouseholdCount: homelessHouseholdCount,
                HousedPeopleCount: housedPeopleCount,
                HomelessPeopleCount: homelessPeopleCount);
        }
    }
}

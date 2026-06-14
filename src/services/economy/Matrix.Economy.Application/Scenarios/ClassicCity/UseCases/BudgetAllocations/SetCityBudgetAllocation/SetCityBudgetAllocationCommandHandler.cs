using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.SetCityBudgetAllocation
{
    public sealed class SetCityBudgetAllocationCommandHandler(
        ICityBudgetRepository budgetRepository,
        ICityBudgetAllocationRepository allocationRepository,
        IEconomyUnitOfWork unitOfWork,
        ICityOperationalBudgetSignalPublisher operationalBudgetSignalPublisher,
        ICityOperationalBudgetPressureProjectionService pressureProjectionService,
        TimeProvider timeProvider)
        : IRequestHandler<SetCityBudgetAllocationCommand, CityBudgetAllocationDto>
    {
        public async Task<CityBudgetAllocationDto> Handle(
            SetCityBudgetAllocationCommand request,
            CancellationToken cancellationToken)
        {
            CityBudgetAllocationDto result = default!;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    CityBudgetUnitProfile requestedUnit = ResolveRequestedUnit(request);

                    CityBudget budget = await budgetRepository.GetByCityAsync(
                                            cityId: request.CityId,
                                            cancellationToken: ct) ??
                                        CreateBudget(
                                            cityId: request.CityId,
                                            requestedUnit: requestedUnit,
                                            budgetRepository: budgetRepository);
                    budget.EnsureCompatibleUnit(requestedUnit);

                    DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();
                    CityBudgetAllocation? allocation = await allocationRepository.GetByCityAndCategoryAsync(
                        cityId: request.CityId,
                        category: request.Category,
                        cancellationToken: ct);

                    if (allocation is null)
                    {
                        allocation = new CityBudgetAllocation(
                            id: Guid.NewGuid(),
                            cityId: request.CityId,
                            category: request.Category,
                            createdAtUtc: updatedAtUtc,
                            unitProfile: budget.GetUnitProfile(),
                            targetAmount: Money.FromDecimal(request.TargetAmount));

                        allocationRepository.Add(allocation);
                    }
                    else
                    {
                        allocation.EnsureCompatibleUnit(budget.GetUnitProfile());
                        allocation.SetTargetAmount(
                            targetAmount: Money.FromDecimal(request.TargetAmount),
                            updatedAtUtc: updatedAtUtc);
                    }

                    await unitOfWork.SaveChangesAsync(ct);

                    CityOperationalBudgetPressureDto pressure = await pressureProjectionService.GetAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);
                    await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                        snapshot: pressure,
                        effectiveAtUtc: updatedAtUtc,
                        occurredAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    result = GetCityBudgetAllocationsQueryHandler.Map(allocation);
                },
                cancellationToken: cancellationToken);

            return result;
        }

        private static CityBudget CreateBudget(
            Guid cityId,
            CityBudgetUnitProfile requestedUnit,
            ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: cityId,
                unitProfile: requestedUnit);
            budgetRepository.Add(budget);
            return budget;
        }

        private static CityBudgetUnitProfile ResolveRequestedUnit(SetCityBudgetAllocationCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.UnitCode) &&
                string.IsNullOrWhiteSpace(request.UnitDisplayName) &&
                string.IsNullOrWhiteSpace(request.UnitSymbol) &&
                string.IsNullOrWhiteSpace(request.UnitKind))
                return CityBudgetUnitProfile.DefaultMoney();

            if (!Enum.TryParse(
                    value: request.UnitKind,
                    ignoreCase: true,
                    result: out CityBudgetUnitKind unitKind))
                throw new InvalidOperationException($"Unsupported unit kind '{request.UnitKind}'.");

            return new CityBudgetUnitProfile(
                Kind: unitKind,
                Code: request.UnitCode ?? string.Empty,
                DisplayName: request.UnitDisplayName ?? string.Empty,
                Symbol: request.UnitSymbol ?? string.Empty);
        }
    }
}

using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetAllocations.SetCityBudgetAllocation
{
    public sealed class SetCityBudgetAllocationCommandHandler(
        ICityBudgetRepository budgetRepository,
        ICityBudgetAllocationRepository allocationRepository,
        IEconomyUnitOfWork unitOfWork,
        ICityOperationalBudgetSignalPublisher operationalBudgetSignalPublisher,
        ISender sender)
        : IRequestHandler<SetCityBudgetAllocationCommand, CityBudgetAllocationDto>
    {
        public async Task<CityBudgetAllocationDto> Handle(
            SetCityBudgetAllocationCommand request,
            CancellationToken cancellationToken)
        {
            CityBudgetUnitProfile requestedUnit = ResolveRequestedUnit(request);

            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: request.CityId,
                                    cancellationToken: cancellationToken) ??
                                CreateBudget(
                                    cityId: request.CityId,
                                    requestedUnit: requestedUnit,
                                    budgetRepository: budgetRepository);
            budget.EnsureCompatibleUnit(requestedUnit);

            DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
            CityBudgetAllocation? allocation = await allocationRepository.GetByCityAndCategoryAsync(
                cityId: request.CityId,
                category: request.Category,
                cancellationToken: cancellationToken);

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

            await unitOfWork.SaveChangesAsync(cancellationToken);
            CityOperationalBudgetPressureDto pressure = await sender.Send(
                request: new GetCityOperationalBudgetPressureQuery(request.CityId),
                cancellationToken: cancellationToken);
            await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                snapshot: pressure,
                effectiveAtUtc: updatedAtUtc,
                occurredAtUtc: DateTimeOffset.UtcNow,
                cancellationToken: cancellationToken);

            return GetCityBudgetAllocationsQueryHandler.Map(allocation);
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

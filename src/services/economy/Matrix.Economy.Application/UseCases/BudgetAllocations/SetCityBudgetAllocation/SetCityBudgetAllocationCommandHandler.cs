using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
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
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<SetCityBudgetAllocationCommand, CityBudgetAllocationDto>
    {
        public async Task<CityBudgetAllocationDto> Handle(
            SetCityBudgetAllocationCommand request,
            CancellationToken cancellationToken)
        {
            CityBudgetUnitProfile requestedUnit = ResolveRequestedUnit(request);

            CityBudget budget = await budgetRepository.GetByCityAsync(request.CityId, cancellationToken)
                ?? CreateBudget(request.CityId, requestedUnit, budgetRepository);
            budget.EnsureCompatibleUnit(requestedUnit);

            DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
            CityBudgetAllocation? allocation = await allocationRepository.GetByCityAndCategoryAsync(
                request.CityId,
                request.Category,
                cancellationToken);

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
                allocation.SetTargetAmount(Money.FromDecimal(request.TargetAmount), updatedAtUtc);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return GetCityBudgetAllocations.GetCityBudgetAllocationsQueryHandler.Map(allocation);
        }

        private static CityBudget CreateBudget(
            Guid cityId,
            CityBudgetUnitProfile requestedUnit,
            ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), cityId, requestedUnit);
            budgetRepository.Add(budget);
            return budget;
        }

        private static CityBudgetUnitProfile ResolveRequestedUnit(SetCityBudgetAllocationCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.UnitCode)
                && string.IsNullOrWhiteSpace(request.UnitDisplayName)
                && string.IsNullOrWhiteSpace(request.UnitSymbol)
                && string.IsNullOrWhiteSpace(request.UnitKind))
            {
                return CityBudgetUnitProfile.DefaultMoney();
            }

            if (!Enum.TryParse(request.UnitKind, ignoreCase: true, out CityBudgetUnitKind unitKind))
            {
                throw new InvalidOperationException($"Unsupported unit kind '{request.UnitKind}'.");
            }

            return new CityBudgetUnitProfile(
                Kind: unitKind,
                Code: request.UnitCode ?? string.Empty,
                DisplayName: request.UnitDisplayName ?? string.Empty,
                Symbol: request.UnitSymbol ?? string.Empty);
        }
    }
}

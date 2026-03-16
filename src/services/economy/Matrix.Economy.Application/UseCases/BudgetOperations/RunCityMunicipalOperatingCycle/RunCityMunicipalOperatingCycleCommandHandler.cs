using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Services;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed class RunCityMunicipalOperatingCycleCommandHandler(
        ICityBudgetAllocationRepository allocationRepository,
        ICityBusinessRepository businessRepository,
        CityMunicipalOperatingCyclePolicy policy,
        CityBudgetBusinessDisbursementSupport disbursementSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RunCityMunicipalOperatingCycleCommand, RunCityMunicipalOperatingCycleResultDto>
    {
        public async Task<RunCityMunicipalOperatingCycleResultDto> Handle(
            RunCityMunicipalOperatingCycleCommand request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBudgetAllocation> allocations = await allocationRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            int allocationCategoriesTouched = 0;
            int providerPayments = 0;
            decimal totalDisbursedAmount = 0m;

            foreach (CityBudgetAllocation allocation in allocations)
            {
                IReadOnlyList<CityMunicipalOperatingDisbursementDecision> decisions =
                    policy.BuildDisbursements(
                        allocation: allocation,
                        businesses: businesses);
                if (decisions.Count == 0)
                    continue;

                allocationCategoriesTouched++;

                foreach (CityMunicipalOperatingDisbursementDecision decision in decisions)
                {
                    CityBusiness? business = businesses.FirstOrDefault(x => x.Id == decision.BusinessId);
                    if (business is null)
                        continue;

                    await disbursementSupport.DisburseAsync(
                        business: business,
                        category: allocation.Category,
                        amount: decision.Amount,
                        title: $"{allocation.Category} operating disbursement",
                        description: "Recurring municipal operating cycle.",
                        cancellationToken: cancellationToken);

                    providerPayments++;
                    totalDisbursedAmount += decision.Amount;
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RunCityMunicipalOperatingCycleResultDto(
                CityId: request.CityId,
                AllocationCategoriesTouched: allocationCategoriesTouched,
                ProviderPayments: providerPayments,
                TotalDisbursedAmount: totalDisbursedAmount);
        }
    }
}

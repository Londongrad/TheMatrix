using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class Job
    {
        public WorkplaceId WorkplaceId { get; }
        public decimal GrossMonthlySalary { get; }
        public decimal IncomeTaxRate { get; }

        public Job(WorkplaceId workplaceId, decimal grossMonthlySalary, decimal incomeTaxRate)
        {
            WorkplaceId = workplaceId;
            GrossMonthlySalary = GuardHelper.AgainstNegativeNumber(grossMonthlySalary, nameof(GrossMonthlySalary));
            IncomeTaxRate = GuardHelper.AgainstOutOfRange(incomeTaxRate, 0, 1, nameof(IncomeTaxRate));
        }

        public MonthlyIncome CalculateMonthlyIncome()
        {
            return MonthlyIncome.FromGrossAndTaxRate(GrossMonthlySalary, IncomeTaxRate);
        }
    }
}

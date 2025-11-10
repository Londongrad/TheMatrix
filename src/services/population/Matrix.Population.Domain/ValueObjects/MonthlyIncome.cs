using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class MonthlyIncome
    {
        public decimal Gross { get; }
        public decimal Net { get; }
        public decimal Tax { get; }

        private MonthlyIncome(decimal gross, decimal net, decimal tax)
        {
            if (gross < 0)
                throw new DomainValidationException("Gross cannot be negative.", nameof(Gross));
            if (net < 0)
                throw new DomainValidationException("Net cannot be negative.", nameof(Net));
            if (tax < 0)
                throw new DomainValidationException("Tax cannot be negative.", nameof(Tax));

            if (gross != net + tax)
                throw new DomainValidationException("Gross must equal Net + Tax.", nameof(Gross));

            Gross = gross;
            Net = net;
            Tax = tax;
        }

        public static MonthlyIncome FromGrossAndTaxRate(decimal gross, decimal taxRate)
        {
            if (gross < 0)
                throw new DomainValidationException("Gross cannot be negative.", nameof(gross));

            if (taxRate < 0 || taxRate > 1)
                throw new DomainValidationException("Tax rate must be between 0 and 1.", nameof(taxRate));

            var tax = Math.Round(gross * taxRate, 2);
            var net = gross - tax;

            return new MonthlyIncome(gross, net, tax);
        }
    }
}

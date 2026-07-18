using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityResidentEconomicContext
    {
        private CityResidentEconomicContext(
            Money dailyTransferIncome,
            decimal retailStoreSpendShareAdjustment,
            decimal serviceSpendShareAdjustment,
            decimal municipalSpendShareAdjustment)
        {
            DailyTransferIncome = dailyTransferIncome;
            RetailStoreSpendShareAdjustment = retailStoreSpendShareAdjustment;
            ServiceSpendShareAdjustment = serviceSpendShareAdjustment;
            MunicipalSpendShareAdjustment = municipalSpendShareAdjustment;
        }

        public static CityResidentEconomicContext Neutral { get; } = new(
            dailyTransferIncome: Money.Zero,
            retailStoreSpendShareAdjustment: 0m,
            serviceSpendShareAdjustment: 0m,
            municipalSpendShareAdjustment: 0m);

        public Money DailyTransferIncome { get; }
        public decimal RetailStoreSpendShareAdjustment { get; }
        public decimal ServiceSpendShareAdjustment { get; }
        public decimal MunicipalSpendShareAdjustment { get; }

        public static CityResidentEconomicContext Create(
            Money dailyTransferIncome,
            decimal retailStoreSpendShareAdjustment,
            decimal serviceSpendShareAdjustment,
            decimal municipalSpendShareAdjustment)
        {
            ArgumentNullException.ThrowIfNull(dailyTransferIncome);

            if (dailyTransferIncome.IsNegative)
                throw new ArgumentOutOfRangeException(nameof(dailyTransferIncome));

            ValidateShareAdjustment(
                value: retailStoreSpendShareAdjustment,
                parameterName: nameof(retailStoreSpendShareAdjustment));
            ValidateShareAdjustment(
                value: serviceSpendShareAdjustment,
                parameterName: nameof(serviceSpendShareAdjustment));
            ValidateShareAdjustment(
                value: municipalSpendShareAdjustment,
                parameterName: nameof(municipalSpendShareAdjustment));

            decimal adjustmentTotal = retailStoreSpendShareAdjustment +
                                      serviceSpendShareAdjustment +
                                      municipalSpendShareAdjustment;
            if (adjustmentTotal != 0m)
                throw new ArgumentException(
                    "Resident spend-share adjustments must preserve the total allocation.");

            return new CityResidentEconomicContext(
                dailyTransferIncome: dailyTransferIncome,
                retailStoreSpendShareAdjustment: retailStoreSpendShareAdjustment,
                serviceSpendShareAdjustment: serviceSpendShareAdjustment,
                municipalSpendShareAdjustment: municipalSpendShareAdjustment);
        }

        private static void ValidateShareAdjustment(
            decimal value,
            string parameterName)
        {
            if (value is < -1m or > 1m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

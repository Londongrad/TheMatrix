namespace Matrix.Population.Domain.Models
{
    public sealed class ResidentAgeIncomeSchedule
    {
        private readonly (int MinimumAge, decimal DailyIncome)[] _bands;

        private ResidentAgeIncomeSchedule((int MinimumAge, decimal DailyIncome)[] bands)
        {
            _bands = bands;
            Bands = Array.AsReadOnly(bands);
        }

        public IReadOnlyList<(int MinimumAge, decimal DailyIncome)> Bands { get; }

        public static ResidentAgeIncomeSchedule None { get; } = new([(0, 0m)]);

        public static ResidentAgeIncomeSchedule Create(
            params (int MinimumAge, decimal DailyIncome)[] bands)
        {
            ArgumentNullException.ThrowIfNull(bands);
            if (bands.Length == 0 || bands[0].MinimumAge != 0)
                throw new ArgumentException("Income schedules must start at age zero.", nameof(bands));

            var storedBands = new (int MinimumAge, decimal DailyIncome)[bands.Length];
            for (int index = 0; index < bands.Length; index++)
            {
                (int minimumAge, decimal dailyIncome) = bands[index];
                if (minimumAge < 0 || (index > 0 && minimumAge <= bands[index - 1].MinimumAge))
                    throw new ArgumentException("Income ages must be strictly increasing.", nameof(bands));
                if (dailyIncome < 0m)
                    throw new ArgumentOutOfRangeException(nameof(bands), "Daily income cannot be negative.");

                storedBands[index] = (minimumAge, dailyIncome);
            }

            return new ResidentAgeIncomeSchedule(storedBands);
        }

        public decimal Resolve(int ageYears)
        {
            if (ageYears < 0)
                throw new ArgumentOutOfRangeException(nameof(ageYears));

            int lower = 0;
            int upper = _bands.Length - 1;
            while (lower < upper)
            {
                int middle = lower + (upper - lower + 1) / 2;
                if (_bands[middle].MinimumAge <= ageYears)
                    lower = middle;
                else
                    upper = middle - 1;
            }

            return _bands[lower].DailyIncome;
        }
    }
}

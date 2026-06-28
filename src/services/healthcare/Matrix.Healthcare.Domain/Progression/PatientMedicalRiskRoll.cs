using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientMedicalRiskRoll
    {
        public bool Occurs(
            PatientId patientId,
            DateOnly currentDate,
            int salt,
            double chancePerReview,
            int reviewWindows)
        {
            if (chancePerReview is < 0d or > 1d)
                throw new ArgumentOutOfRangeException(nameof(chancePerReview));
            if (reviewWindows < 0)
                throw new ArgumentOutOfRangeException(nameof(reviewWindows));
            if (reviewWindows == 0 || chancePerReview == 0d)
                return false;

            double combinedChance = 1d - Math.Pow(1d - chancePerReview, reviewWindows);
            return GetStableFraction(patientId, currentDate, salt) < combinedChance;
        }

        private static double GetStableFraction(
            PatientId patientId,
            DateOnly currentDate,
            int salt)
        {
            unchecked
            {
                byte[] bytes = patientId.Value.ToByteArray();
                int hash = 19;
                for (int index = 0; index < bytes.Length; index++)
                    hash = (hash * 31) + bytes[index];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (Math.Abs((long)hash) % 10_000) / 10_000d;
            }
        }
    }
}

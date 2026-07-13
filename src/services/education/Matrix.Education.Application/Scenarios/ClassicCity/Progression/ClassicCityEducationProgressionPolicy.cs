using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Progression
{
    public sealed class ClassicCityEducationProgressionPolicy
    {
        private const int ReviewWindowDays = 30;

        public bool TryResolveInferredBaseline(
            StudentProfile profile,
            DateOnly currentDate,
            out EducationStageKey stage,
            out DateOnly completedOn)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (profile.CompletedStage.HasValue || currentDate < profile.BirthDate)
            {
                stage = default;
                completedOn = default;
                return false;
            }

            if (currentDate >= profile.BirthDate.AddYears(18))
            {
                stage = ClassicCityEducationStageCatalog.UpperSecondary;
                completedOn = profile.BirthDate.AddYears(18);
                return true;
            }

            if (currentDate >= profile.BirthDate.AddYears(16))
            {
                stage = ClassicCityEducationStageCatalog.LowerSecondary;
                completedOn = profile.BirthDate.AddYears(16);
                return true;
            }

            if (currentDate >= profile.BirthDate.AddYears(13))
            {
                stage = ClassicCityEducationStageCatalog.Primary;
                completedOn = profile.BirthDate.AddYears(13);
                return true;
            }

            if (currentDate >= profile.BirthDate.AddYears(7))
            {
                stage = ClassicCityEducationStageCatalog.Preschool;
                completedOn = profile.BirthDate.AddYears(7);
                return true;
            }

            stage = default;
            completedOn = default;
            return false;
        }

        public EducationStageKey? ResolveNextEnrollmentStage(
            StudentProfile profile,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (!profile.IsAlive || !profile.IsActive || currentDate < profile.BirthDate.AddYears(3))
                return null;

            if (!profile.CompletedStage.HasValue)
                return ClassicCityEducationStageCatalog.Preschool;

            EducationStageKey completedStage = profile.CompletedStage.Value;
            if (completedStage == ClassicCityEducationStageCatalog.Preschool &&
                currentDate >= profile.BirthDate.AddYears(7))
                return ClassicCityEducationStageCatalog.Primary;
            if (completedStage == ClassicCityEducationStageCatalog.Primary &&
                currentDate >= profile.BirthDate.AddYears(13))
                return ClassicCityEducationStageCatalog.LowerSecondary;
            if (completedStage == ClassicCityEducationStageCatalog.LowerSecondary &&
                currentDate >= profile.BirthDate.AddYears(16))
                return ClassicCityEducationStageCatalog.UpperSecondary;

            int age = ResolveAge(profile.BirthDate, currentDate);
            if (completedStage == ClassicCityEducationStageCatalog.UpperSecondary && age is >= 18 and <= 23)
                return PassesStableReview(
                    profile.ResidentId.Value,
                    currentDate,
                    salt: 1_973,
                    chancePerWindow: 0.05d)
                    ? ClassicCityEducationStageCatalog.Vocational
                    : null;
            if (completedStage == ClassicCityEducationStageCatalog.Vocational && age <= 25)
                return PassesStableReview(
                    profile.ResidentId.Value,
                    currentDate,
                    salt: 1_921,
                    chancePerWindow: 0.04d)
                    ? ClassicCityEducationStageCatalog.Higher
                    : null;
            if (completedStage == ClassicCityEducationStageCatalog.Higher && age <= 29)
                return PassesStableReview(
                    profile.ResidentId.Value,
                    currentDate,
                    salt: 1_949,
                    chancePerWindow: 0.02d)
                    ? ClassicCityEducationStageCatalog.Postgraduate
                    : null;

            return null;
        }

        public DateOnly? ResolveCompletionDate(
            StudentProfile profile,
            EducationStageKey stage,
            DateOnly enrolledOn)
        {
            ArgumentNullException.ThrowIfNull(profile);

            DateOnly? target = stage == ClassicCityEducationStageCatalog.Preschool
                ? profile.BirthDate.AddYears(7)
                : stage == ClassicCityEducationStageCatalog.Primary
                    ? profile.BirthDate.AddYears(13)
                    : stage == ClassicCityEducationStageCatalog.LowerSecondary
                        ? profile.BirthDate.AddYears(16)
                        : stage == ClassicCityEducationStageCatalog.UpperSecondary
                            ? profile.BirthDate.AddYears(18)
                            : stage == ClassicCityEducationStageCatalog.Vocational
                                ? enrolledOn.AddYears(2)
                                : stage == ClassicCityEducationStageCatalog.Higher
                                    ? enrolledOn.AddYears(4)
                                    : stage == ClassicCityEducationStageCatalog.Postgraduate
                                        ? enrolledOn.AddYears(3)
                                        : null;

            return target.HasValue && target.Value < enrolledOn
                ? enrolledOn
                : target;
        }

        private static int ResolveAge(DateOnly birthDate, DateOnly currentDate)
        {
            int age = currentDate.Year - birthDate.Year;
            return currentDate < birthDate.AddYears(age)
                ? age - 1
                : age;
        }

        private static bool PassesStableReview(
            Guid residentId,
            DateOnly currentDate,
            int salt,
            double chancePerWindow)
        {
            int reviewWindow = currentDate.DayNumber / ReviewWindowDays;
            ulong hash = 14_695_981_039_346_656_037UL;
            Span<byte> residentBytes = stackalloc byte[16];
            residentId.TryWriteBytes(residentBytes);

            foreach (byte value in residentBytes)
                Mix(ref hash, value);
            Mix(ref hash, unchecked((uint)reviewWindow));
            Mix(ref hash, unchecked((uint)salt));

            double fraction = (hash >> 11) * (1d / (1UL << 53));
            return fraction < chancePerWindow;
        }

        private static void Mix(ref ulong hash, uint value)
        {
            Mix(ref hash, (byte)value);
            Mix(ref hash, (byte)(value >> 8));
            Mix(ref hash, (byte)(value >> 16));
            Mix(ref hash, (byte)(value >> 24));
        }

        private static void Mix(ref ulong hash, byte value)
        {
            hash ^= value;
            hash *= 1_099_511_628_211UL;
        }
    }
}

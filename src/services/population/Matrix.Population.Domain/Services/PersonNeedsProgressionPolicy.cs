using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services
{
    public sealed class PersonNeedsProgressionPolicy
    {
        private static readonly TimeSpan SleepStart = TimeSpan.FromHours(22);
        private static readonly TimeSpan SleepEnd = TimeSpan.FromHours(6);
        private static readonly TimeSpan ActivityStart = TimeSpan.FromHours(8);
        private static readonly TimeSpan StudentActivityEnd = TimeSpan.FromHours(15);
        private static readonly TimeSpan EmploymentActivityEnd = TimeSpan.FromHours(17);
        private static readonly TimeSpan[] PhaseBoundaries =
        {
            SleepEnd,
            ActivityStart,
            StudentActivityEnd,
            EmploymentActivityEnd,
            SleepStart
        };

        public PersonNeedsProgressionEffect Calculate(
            Person person,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            int utcOffsetMinutes)
        {
            person = GuardHelper.AgainstNull(
                value: person,
                propertyName: nameof(person));

            if (!person.IsAlive || toSimTimeUtc <= fromSimTimeUtc)
                return PersonNeedsProgressionEffect.None;

            TimeSpan utcOffset = TimeSpan.FromMinutes(utcOffsetMinutes);
            DateTimeOffset localCursor = fromSimTimeUtc.ToOffset(utcOffset);
            DateTimeOffset localEnd = toSimTimeUtc.ToOffset(utcOffset);

            decimal energy = person.Energy.Value;
            decimal stress = person.Stress.Value;
            decimal socialNeed = person.SocialNeed.Value;

            decimal totalEnergyDelta = 0;
            decimal totalStressDelta = 0;
            decimal totalSocialNeedDelta = 0;
            decimal totalHealthDelta = 0;
            decimal totalHappinessDelta = 0;

            while (localCursor < localEnd)
            {
                PersonRoutinePhase phase = ResolvePhase(
                    person: person,
                    localTime: localCursor);
                DateTimeOffset segmentEnd = Min(
                    left: localEnd,
                    right: GetNextBoundary(localCursor));
                decimal hours = (decimal)(segmentEnd - localCursor).TotalHours;

                ApplyPhase(
                    person: person,
                    phase: phase,
                    hours: hours,
                    localDate: DateOnly.FromDateTime(localCursor.DateTime),
                    energy: ref energy,
                    stress: ref stress,
                    socialNeed: ref socialNeed,
                    totalEnergyDelta: ref totalEnergyDelta,
                    totalStressDelta: ref totalStressDelta,
                    totalSocialNeedDelta: ref totalSocialNeedDelta,
                    totalHealthDelta: ref totalHealthDelta,
                    totalHappinessDelta: ref totalHappinessDelta);

                localCursor = segmentEnd;
            }

            return new PersonNeedsProgressionEffect(
                EnergyDelta: RoundToInt(totalEnergyDelta),
                StressDelta: RoundToInt(totalStressDelta),
                SocialNeedDelta: RoundToInt(totalSocialNeedDelta),
                HealthDelta: RoundToInt(totalHealthDelta),
                HappinessDelta: RoundToInt(totalHappinessDelta));
        }

        private static void ApplyPhase(
            Person person,
            PersonRoutinePhase phase,
            decimal hours,
            DateOnly localDate,
            ref decimal energy,
            ref decimal stress,
            ref decimal socialNeed,
            ref decimal totalEnergyDelta,
            ref decimal totalStressDelta,
            ref decimal totalSocialNeedDelta,
            ref decimal totalHealthDelta,
            ref decimal totalHappinessDelta)
        {
            if (hours <= 0)
                return;

            RoutineDrift drift = BuildDrift(
                person: person,
                phase: phase,
                localDate: localDate);

            decimal energyBefore = energy;
            decimal stressBefore = stress;
            decimal socialNeedBefore = socialNeed;

            energy = ClampNeed(energy + (drift.EnergyPerHour * hours));
            stress = ClampNeed(stress + (drift.StressPerHour * hours));
            socialNeed = ClampNeed(socialNeed + (drift.SocialNeedPerHour * hours));

            totalEnergyDelta += energy - energyBefore;
            totalStressDelta += stress - stressBefore;
            totalSocialNeedDelta += socialNeed - socialNeedBefore;

            ApplySecondaryEffects(
                phase: phase,
                hours: hours,
                averageEnergy: (energyBefore + energy) / 2m,
                averageStress: (stressBefore + stress) / 2m,
                averageSocialNeed: (socialNeedBefore + socialNeed) / 2m,
                totalHealthDelta: ref totalHealthDelta,
                totalHappinessDelta: ref totalHappinessDelta);
        }

        private static RoutineDrift BuildDrift(
            Person person,
            PersonRoutinePhase phase,
            DateOnly localDate)
        {
            AgeGroup ageGroup = person.GetAgeGroup(localDate);
            decimal disciplineFactor = 1m - ((person.Personality.Discipline - 50) / 250m);
            decimal contactsFactor = person.Personality.GetContactsFactor();
            decimal socialPressureFactor = 0.75m + (person.Personality.Sociability / 100m);

            return phase switch
            {
                PersonRoutinePhase.Sleep => new RoutineDrift(
                    EnergyPerHour: +6.0m,
                    StressPerHour: -3.0m,
                    SocialNeedPerHour: 0.10m * socialPressureFactor),
                PersonRoutinePhase.StructuredActivity when person.Employment.Status == EmploymentStatus.Student => new RoutineDrift(
                    EnergyPerHour: -3.5m * disciplineFactor,
                    StressPerHour: +2.4m * disciplineFactor,
                    SocialNeedPerHour: -0.85m * contactsFactor),
                PersonRoutinePhase.StructuredActivity => new RoutineDrift(
                    EnergyPerHour: -4.2m * disciplineFactor,
                    StressPerHour: +3.2m * disciplineFactor,
                    SocialNeedPerHour: -1.00m * contactsFactor),
                _ => BuildLeisureDrift(
                    person: person,
                    ageGroup: ageGroup,
                    contactsFactor: contactsFactor,
                    socialPressureFactor: socialPressureFactor)
            };
        }

        private static RoutineDrift BuildLeisureDrift(
            Person person,
            AgeGroup ageGroup,
            decimal contactsFactor,
            decimal socialPressureFactor)
        {
            decimal energyDrain = ageGroup switch
            {
                AgeGroup.Child => -0.8m,
                AgeGroup.Youth => -1.0m,
                AgeGroup.Adult => -1.2m,
                AgeGroup.Senior => -1.0m,
                _ => -1.1m
            };

            decimal socialNeedPerHour;
            if (person.MaritalStatus == MaritalStatus.Married)
            {
                socialNeedPerHour = -0.70m * contactsFactor;
            }
            else if (ageGroup is AgeGroup.Child or AgeGroup.Youth)
            {
                socialNeedPerHour = -0.35m * contactsFactor;
            }
            else
            {
                decimal basePressure = person.Employment.Status switch
                {
                    EmploymentStatus.Unemployed => 0.90m,
                    EmploymentStatus.Retired => 0.70m,
                    EmploymentStatus.None => 0.60m,
                    _ => 0.30m
                };

                if (person.MaritalStatus is MaritalStatus.Divorced or MaritalStatus.Widowed)
                    basePressure += 0.20m;

                socialNeedPerHour = basePressure * socialPressureFactor;
            }

            return new RoutineDrift(
                EnergyPerHour: energyDrain,
                StressPerHour: -1.0m,
                SocialNeedPerHour: socialNeedPerHour);
        }

        private static void ApplySecondaryEffects(
            PersonRoutinePhase phase,
            decimal hours,
            decimal averageEnergy,
            decimal averageStress,
            decimal averageSocialNeed,
            ref decimal totalHealthDelta,
            ref decimal totalHappinessDelta)
        {
            if (averageEnergy < 15)
            {
                totalHealthDelta -= 0.45m * hours;
                totalHappinessDelta -= 0.90m * hours;
            }
            else if (averageEnergy < 30)
            {
                totalHappinessDelta -= 0.35m * hours;
            }

            if (averageStress > 85)
            {
                totalHealthDelta -= 0.30m * hours;
                totalHappinessDelta -= 0.65m * hours;
            }
            else if (averageStress > 70)
            {
                totalHappinessDelta -= 0.30m * hours;
            }

            if (averageSocialNeed > 80)
            {
                totalHappinessDelta -= 0.55m * hours;
            }
            else if (averageSocialNeed > 65)
            {
                totalHappinessDelta -= 0.20m * hours;
            }

            if ((phase is PersonRoutinePhase.Sleep or PersonRoutinePhase.Leisure) &&
                averageEnergy > 65 &&
                averageStress < 35 &&
                averageSocialNeed < 45)
                totalHappinessDelta += 0.20m * hours;
        }

        private static PersonRoutinePhase ResolvePhase(
            Person person,
            DateTimeOffset localTime)
        {
            TimeSpan timeOfDay = localTime.TimeOfDay;
            if (timeOfDay >= SleepStart || timeOfDay < SleepEnd)
                return PersonRoutinePhase.Sleep;

            bool isWeekday = localTime.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            if (isWeekday)
            {
                if (person.Employment.Status == EmploymentStatus.Student &&
                    timeOfDay >= ActivityStart &&
                    timeOfDay < StudentActivityEnd)
                    return PersonRoutinePhase.StructuredActivity;

                if (person.Employment.Status == EmploymentStatus.Employed &&
                    timeOfDay >= ActivityStart &&
                    timeOfDay < EmploymentActivityEnd)
                    return PersonRoutinePhase.StructuredActivity;
            }

            return PersonRoutinePhase.Leisure;
        }

        private static DateTimeOffset GetNextBoundary(DateTimeOffset localTime)
        {
            foreach (TimeSpan boundary in PhaseBoundaries)
            {
                DateTimeOffset candidate = CreateBoundary(
                    localTime: localTime,
                    boundary: boundary);

                if (candidate > localTime)
                    return candidate;
            }

            return new DateTimeOffset(
                year: localTime.Year,
                month: localTime.Month,
                day: localTime.Day,
                hour: 0,
                minute: 0,
                second: 0,
                offset: localTime.Offset).AddDays(1);
        }

        private static DateTimeOffset CreateBoundary(
            DateTimeOffset localTime,
            TimeSpan boundary)
        {
            return new DateTimeOffset(
                year: localTime.Year,
                month: localTime.Month,
                day: localTime.Day,
                hour: boundary.Hours,
                minute: boundary.Minutes,
                second: boundary.Seconds,
                offset: localTime.Offset);
        }

        private static DateTimeOffset Min(
            DateTimeOffset left,
            DateTimeOffset right)
        {
            return left <= right
                ? left
                : right;
        }

        private static decimal ClampNeed(decimal value)
        {
            return Math.Clamp(
                value: value,
                min: 0m,
                max: 100m);
        }

        private static int RoundToInt(decimal value)
        {
            return (int)Math.Round(
                value,
                MidpointRounding.AwayFromZero);
        }

        private enum PersonRoutinePhase
        {
            Sleep = 1,
            StructuredActivity = 2,
            Leisure = 3
        }

        private readonly record struct RoutineDrift(
            decimal EnergyPerHour,
            decimal StressPerHour,
            decimal SocialNeedPerHour);
    }
}

using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationClimateAdaptationPolicy
    {
        public int GetToleranceScore(
            CityPopulationEnvironment? environment,
            DateOnly currentDate,
            PopulationWeatherType weatherType)
        {
            if (environment is null)
                return 0;

            PopulationSeason season = GetSeason(
                date: currentDate,
                hemisphere: environment.Hemisphere);

            int climateScore = weatherType switch
            {
                PopulationWeatherType.Heatwave => environment.ClimateZone switch
                {
                    PopulationClimateZone.Tropical => 2,
                    PopulationClimateZone.Arid => 2,
                    PopulationClimateZone.Temperate => 1,
                    PopulationClimateZone.Continental => 0,
                    PopulationClimateZone.Mountain => -1,
                    PopulationClimateZone.Polar => -2,
                    _ => 0
                },
                PopulationWeatherType.ColdSnap => environment.ClimateZone switch
                {
                    PopulationClimateZone.Polar => 2,
                    PopulationClimateZone.Continental => 2,
                    PopulationClimateZone.Mountain => 1,
                    PopulationClimateZone.Temperate => 0,
                    PopulationClimateZone.Arid => -1,
                    PopulationClimateZone.Tropical => -2,
                    _ => 0
                },
                PopulationWeatherType.Snow => environment.ClimateZone switch
                {
                    PopulationClimateZone.Polar => 2,
                    PopulationClimateZone.Continental => 2,
                    PopulationClimateZone.Mountain => 2,
                    PopulationClimateZone.Temperate => 1,
                    PopulationClimateZone.Arid => -1,
                    PopulationClimateZone.Tropical => -2,
                    _ => 0
                },
                PopulationWeatherType.Storm => environment.ClimateZone switch
                {
                    PopulationClimateZone.Tropical => 1,
                    PopulationClimateZone.Temperate => 1,
                    PopulationClimateZone.Continental => 0,
                    PopulationClimateZone.Polar => 0,
                    PopulationClimateZone.Mountain => -1,
                    PopulationClimateZone.Arid => -1,
                    _ => 0
                },
                _ => 0
            };

            int seasonScore = weatherType switch
            {
                PopulationWeatherType.Heatwave => season switch
                {
                    PopulationSeason.Summer => 1,
                    PopulationSeason.Winter => -1,
                    _ => 0
                },
                PopulationWeatherType.ColdSnap => season switch
                {
                    PopulationSeason.Winter => 1,
                    PopulationSeason.Summer => -1,
                    _ => 0
                },
                PopulationWeatherType.Snow => season switch
                {
                    PopulationSeason.Winter => 1,
                    PopulationSeason.Summer => -2,
                    _ => 0
                },
                _ => 0
            };

            return Math.Clamp(
                value: climateScore + seasonScore,
                min: -2,
                max: 2);
        }

        public int AdjustNegativeDelta(
            int delta,
            int toleranceScore)
        {
            if (delta >= 0)
                return delta;

            return Math.Min(
                val1: 0,
                val2: delta + toleranceScore);
        }

        public int AdjustPositiveReliefDelta(
            int delta,
            int toleranceScore)
        {
            if (delta <= 0)
                return delta;

            int sensitivityScore = -toleranceScore;
            return Math.Max(
                val1: 0,
                val2: delta + sensitivityScore);
        }

        public decimal AdjustExposureStepHours(
            decimal stepHours,
            int toleranceScore)
        {
            decimal factor = toleranceScore switch
            {
                2 => 1.50m,
                1 => 1.25m,
                -1 => 0.85m,
                -2 => 0.70m,
                _ => 1.00m
            };

            return Math.Clamp(
                value: stepHours * factor,
                min: 4m,
                max: 72m);
        }

        private static PopulationSeason GetSeason(
            DateOnly date,
            PopulationHemisphere hemisphere)
        {
            int month = date.Month;

            PopulationSeason northernSeason = month switch
            {
                12 or 1 or 2 => PopulationSeason.Winter,
                3 or 4 or 5 => PopulationSeason.Spring,
                6 or 7 or 8 => PopulationSeason.Summer,
                _ => PopulationSeason.Autumn
            };

            if (hemisphere != PopulationHemisphere.Southern)
                return northernSeason;

            return northernSeason switch
            {
                PopulationSeason.Spring => PopulationSeason.Autumn,
                PopulationSeason.Summer => PopulationSeason.Winter,
                PopulationSeason.Autumn => PopulationSeason.Spring,
                PopulationSeason.Winter => PopulationSeason.Summer,
                _ => northernSeason
            };
        }
    }
}

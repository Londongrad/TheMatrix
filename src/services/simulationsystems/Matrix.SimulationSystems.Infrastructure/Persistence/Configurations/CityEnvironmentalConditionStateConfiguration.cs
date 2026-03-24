using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationSystems.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Configurations
{
    public sealed class CityEnvironmentalConditionStateConfiguration
        : IEntityTypeConfiguration<CityEnvironmentalConditionState>
    {
        public void Configure(EntityTypeBuilder<CityEnvironmentalConditionState> builder)
        {
            builder.ToTable("CityEnvironmentalConditions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new SimulationHostId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.LastEvaluatedAtUtc)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.WeatherPressure,
                buildAction: pressure =>
                {
                    pressure.Property(x => x.RainPressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherRainPressure")
                       .IsRequired();

                    pressure.Property(x => x.SnowPressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherSnowPressure")
                       .IsRequired();

                    pressure.Property(x => x.StormPressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherStormPressure")
                       .IsRequired();

                    pressure.Property(x => x.FreezePressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherFreezePressure")
                       .IsRequired();

                    pressure.Property(x => x.ThawRelief)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherThawRelief")
                       .IsRequired();
                });

            builder.Navigation(x => x.WeatherPressure)
               .IsRequired();

            builder.Property(x => x.FloodingIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => FloodingIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.SnowAccumulationIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => SnowAccumulationIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.RoadAccessibilityIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => RoadAccessibilityIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Drainage,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "Drainage"));

            builder.Navigation(x => x.Drainage)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.DrainageInfrastructure,
                buildAction: drainage =>
                {
                    drainage.Property(x => x.PumpCapacityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainagePumpCapacityIndex")
                       .IsRequired();

                    drainage.Property(x => x.NetworkIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageNetworkIntegrityIndex")
                       .IsRequired();

                    drainage.Property(x => x.BlockageIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageBlockageIndex")
                       .IsRequired();

                    drainage.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageCrewReadinessIndex")
                       .IsRequired();

                    drainage.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageIncidentPressureIndex")
                       .IsRequired();

                    drainage.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("DrainageEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.DrainageInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SnowRemoval,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "SnowRemoval"));

            builder.Navigation(x => x.SnowRemoval)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SnowRemovalInfrastructure,
                buildAction: snowRemoval =>
                {
                    snowRemoval.Property(x => x.FleetAvailabilityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalFleetAvailabilityIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.RouteCoverageIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalRouteCoverageIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.DeicingReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalDeicingReadinessIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalCrewReadinessIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalIncidentPressureIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("SnowRemovalEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.SnowRemovalInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.RoadAccess,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "RoadAccess"));

            builder.Navigation(x => x.RoadAccess)
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }

        private static void ConfigureSystemState<TOwner>(
            OwnedNavigationBuilder<TOwner, CitySystemState> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.Kind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}Kind")
               .IsRequired();

            builder.Property(x => x.LoadIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}LoadIndex")
               .IsRequired();

            builder.Property(x => x.ServiceQualityIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}ServiceQualityIndex")
               .IsRequired();

            builder.Property(x => x.BacklogIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}BacklogIndex")
               .IsRequired();

            builder.Property(x => x.FailureRiskIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}FailureRiskIndex")
               .IsRequired();
        }
    }
}

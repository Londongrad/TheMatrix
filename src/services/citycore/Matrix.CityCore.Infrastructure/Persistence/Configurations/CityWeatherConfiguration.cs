using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;
using Matrix.CityCore.Domain.Weather.Profiles;
using Matrix.CityCore.Domain.Weather.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.CityCore.Infrastructure.Persistence.Configurations
{
    public sealed class CityWeatherConfiguration : IEntityTypeConfiguration<CityWeather>
    {
        public void Configure(EntityTypeBuilder<CityWeather> builder)
        {
            builder.ToTable("CityWeathers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.LastEvaluatedAt)
               .HasConversion(
                    convertToProviderExpression: x => x.ValueUtc,
                    convertFromProviderExpression: x => SimTime.FromUtc(x))
               .IsRequired();

            builder.Property(x => x.LastTransitionAt)
               .HasConversion(
                    convertToProviderExpression: x => x.ValueUtc,
                    convertFromProviderExpression: x => SimTime.FromUtc(x))
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.ClimateProfile,
                buildAction: climate =>
                {
                    climate.Property(x => x.ClimateZone)
                       .HasConversion<int>()
                       .HasColumnName("ClimateZone")
                       .IsRequired();

                    climate.Property(x => x.Volatility)
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => WeatherVolatility.From(x))
                       .HasPrecision(
                            precision: 4,
                            scale: 3)
                       .HasColumnName("Volatility")
                       .IsRequired();

                    climate.OwnsOne(
                        navigationExpression: x => x.TemperatureProfile,
                        buildAction: profile => ConfigureTemperatureProfile(
                            builder: profile,
                            prefix: "Temperature"));

                    climate.OwnsOne(
                        navigationExpression: x => x.PrecipitationProfile,
                        buildAction: profile => ConfigurePrecipitationProfile(
                            builder: profile,
                            prefix: "Precipitation"));

                    climate.OwnsOne(
                        navigationExpression: x => x.WindProfile,
                        buildAction: profile => ConfigureWindProfile(
                            builder: profile,
                            prefix: "Wind"));

                    climate.OwnsOne(
                        navigationExpression: x => x.ExtremeWeatherProfile,
                        buildAction: profile => ConfigureExtremeWeatherProfile(
                            builder: profile,
                            prefix: "Extreme"));
                });

            builder.Navigation(x => x.ClimateProfile)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.CurrentState,
                buildAction: state => ConfigureWeatherState(
                    builder: state,
                    prefix: "Current"));

            builder.Navigation(x => x.CurrentState)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.ActiveOverride,
                buildAction: weatherOverride =>
                {
                    weatherOverride.ToTable("CityWeatherOverrides");
                    weatherOverride.WithOwner()
                       .HasForeignKey("CityId");
                    weatherOverride.Property<CityId>("CityId")
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => new CityId(x))
                       .HasColumnName("CityId")
                       .ValueGeneratedNever();

                    weatherOverride.HasKey("CityId");

                    weatherOverride.Property(x => x.Id)
                       .HasColumnName("OverrideId")
                       .ValueGeneratedNever();

                    weatherOverride.Property(x => x.Source)
                       .HasConversion<int>()
                       .HasColumnName("Source")
                       .IsRequired();

                    weatherOverride.Property(x => x.Reason)
                       .HasMaxLength(256)
                       .HasColumnName("Reason")
                       .IsRequired(false);

                    weatherOverride.Property(x => x.StartsAt)
                       .HasConversion(
                            convertToProviderExpression: x => x.ValueUtc,
                            convertFromProviderExpression: x => SimTime.FromUtc(x))
                       .HasColumnName("StartsAt")
                       .IsRequired();

                    weatherOverride.Property(x => x.EndsAt)
                       .HasConversion(
                            convertToProviderExpression: x => x.ValueUtc,
                            convertFromProviderExpression: x => SimTime.FromUtc(x))
                       .HasColumnName("EndsAt")
                       .IsRequired();

                    weatherOverride.OwnsOne(
                        navigationExpression: x => x.ForcedState,
                        buildAction: state => ConfigureWeatherState(
                            builder: state,
                            prefix: "Forced"));
                });

            builder.Navigation(x => x.ActiveOverride)
               .IsRequired(false);

            builder.Ignore(x => x.DomainEvents);

            builder
               .HasOne<City>()
               .WithOne()
               .HasForeignKey<CityWeather>(x => x.Id)
               .OnDelete(DeleteBehavior.Cascade);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }

        private static void ConfigureWeatherState<TOwner>(
            OwnedNavigationBuilder<TOwner, WeatherState> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.Type)
               .HasConversion<int>()
               .HasColumnName($"{prefix}Type")
               .IsRequired();

            builder.Property(x => x.Severity)
               .HasConversion<int>()
               .HasColumnName($"{prefix}Severity")
               .IsRequired();

            builder.Property(x => x.PrecipitationKind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}PrecipitationKind")
               .IsRequired();

            builder.Property(x => x.Temperature)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => TemperatureC.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}TemperatureC")
               .IsRequired();

            builder.Property(x => x.Humidity)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => HumidityPercent.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 2)
               .HasColumnName($"{prefix}HumidityPercent")
               .IsRequired();

            builder.Property(x => x.WindSpeed)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WindSpeedKph.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}WindSpeedKph")
               .IsRequired();

            builder.Property(x => x.CloudCoverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => CloudCoveragePercent.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 2)
               .HasColumnName($"{prefix}CloudCoveragePercent")
               .IsRequired();

            builder.Property(x => x.Pressure)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => PressureHpa.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}PressureHpa")
               .IsRequired();

            builder.Property(x => x.StartedAt)
               .HasConversion(
                    convertToProviderExpression: x => x.ValueUtc,
                    convertFromProviderExpression: x => SimTime.FromUtc(x))
               .HasColumnName($"{prefix}StartedAt")
               .IsRequired();

            builder.Property(x => x.ExpectedUntil)
               .HasConversion(
                    convertToProviderExpression: x => x.ValueUtc,
                    convertFromProviderExpression: x => SimTime.FromUtc(x))
               .HasColumnName($"{prefix}ExpectedUntil")
               .IsRequired();
        }

        private static void ConfigureTemperatureProfile<TOwner>(
            OwnedNavigationBuilder<TOwner, SeasonalTemperatureProfile> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.SpringAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => TemperatureC.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}SpringAverage")
               .IsRequired();

            builder.Property(x => x.SummerAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => TemperatureC.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}SummerAverage")
               .IsRequired();

            builder.Property(x => x.AutumnAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => TemperatureC.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}AutumnAverage")
               .IsRequired();

            builder.Property(x => x.WinterAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => TemperatureC.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}WinterAverage")
               .IsRequired();

            builder.Property(x => x.DailySwing)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => TemperatureC.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}DailySwing")
               .IsRequired();
        }

        private static void ConfigurePrecipitationProfile<TOwner>(
            OwnedNavigationBuilder<TOwner, SeasonalPrecipitationProfile> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.SpringHumidity)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => HumidityPercent.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 2)
               .HasColumnName($"{prefix}SpringHumidity")
               .IsRequired();

            builder.Property(x => x.SummerHumidity)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => HumidityPercent.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 2)
               .HasColumnName($"{prefix}SummerHumidity")
               .IsRequired();

            builder.Property(x => x.AutumnHumidity)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => HumidityPercent.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 2)
               .HasColumnName($"{prefix}AutumnHumidity")
               .IsRequired();

            builder.Property(x => x.WinterHumidity)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => HumidityPercent.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 2)
               .HasColumnName($"{prefix}WinterHumidity")
               .IsRequired();

            builder.Property(x => x.SpringDominantKind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}SpringDominantKind")
               .IsRequired();

            builder.Property(x => x.SummerDominantKind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}SummerDominantKind")
               .IsRequired();

            builder.Property(x => x.AutumnDominantKind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}AutumnDominantKind")
               .IsRequired();

            builder.Property(x => x.WinterDominantKind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}WinterDominantKind")
               .IsRequired();
        }

        private static void ConfigureWindProfile<TOwner>(
            OwnedNavigationBuilder<TOwner, SeasonalWindProfile> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.SpringAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WindSpeedKph.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}SpringAverage")
               .IsRequired();

            builder.Property(x => x.SummerAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WindSpeedKph.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}SummerAverage")
               .IsRequired();

            builder.Property(x => x.AutumnAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WindSpeedKph.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}AutumnAverage")
               .IsRequired();

            builder.Property(x => x.WinterAverage)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WindSpeedKph.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}WinterAverage")
               .IsRequired();

            builder.Property(x => x.GustHeadroom)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WindSpeedKph.From(x))
               .HasPrecision(
                    precision: 6,
                    scale: 2)
               .HasColumnName($"{prefix}GustHeadroom")
               .IsRequired();
        }

        private static void ConfigureExtremeWeatherProfile<TOwner>(
            OwnedNavigationBuilder<TOwner, ExtremeWeatherProfile> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.MaxOverallSeverity)
               .HasConversion<int>()
               .HasColumnName($"{prefix}MaxOverallSeverity")
               .IsRequired();

            builder.Property(x => x.SupportsThunderstorms)
               .HasColumnName($"{prefix}SupportsThunderstorms")
               .IsRequired();

            builder.Property(x => x.SupportsSnowstorms)
               .HasColumnName($"{prefix}SupportsSnowstorms")
               .IsRequired();

            builder.Property(x => x.SupportsFog)
               .HasColumnName($"{prefix}SupportsFog")
               .IsRequired();

            builder.Property(x => x.SupportsHeatwaves)
               .HasColumnName($"{prefix}SupportsHeatwaves")
               .IsRequired();
        }
    }
}

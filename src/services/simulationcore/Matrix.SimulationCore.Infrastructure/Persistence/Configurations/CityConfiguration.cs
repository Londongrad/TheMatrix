using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Configurations
{
    public sealed class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("Cities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.Name)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityName(x))
               .HasMaxLength(CityName.MaxLength)
               .IsRequired();

            builder.Property(x => x.SimulationKind)
               .HasConversion<int>()
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Environment,
                buildAction: environment =>
                {
                    environment.Property(x => x.ClimateZone)
                       .HasConversion<int>()
                       .HasColumnName("ClimateZone")
                       .IsRequired();

                    environment.Property(x => x.Hemisphere)
                       .HasConversion<int>()
                       .HasColumnName("Hemisphere")
                       .IsRequired();

                    environment.Property(x => x.UtcOffset)
                       .HasConversion(
                            convertToProviderExpression: x => x.TotalMinutes,
                            convertFromProviderExpression: x => CityUtcOffset.FromMinutes(x))
                       .HasColumnName("UtcOffsetMinutes")
                       .IsRequired();
                });

            builder.Navigation(x => x.Environment)
               .IsRequired();

            builder.Property(x => x.GenerationSeed)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityGenerationSeed(x))
               .HasMaxLength(CityGenerationSeed.MaxLength)
               .IsRequired();

            builder.Property(x => x.RunId)
               .HasDefaultValueSql("gen_random_uuid()")
               .IsRequired();

            builder.Property(x => x.ScenarioModelSetVersion)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new ScenarioModelSetVersion(x))
               .HasMaxLength(ScenarioModelSetVersion.MaxLength)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.GenerationProfile,
                buildAction: profile =>
                {
                    profile.Property(x => x.SizeTier)
                       .HasConversion<int>()
                       .HasColumnName("GenerationSizeTier")
                       .IsRequired();

                    profile.Property(x => x.UrbanDensity)
                       .HasConversion<int>()
                       .HasColumnName("GenerationUrbanDensity")
                       .IsRequired();

                    profile.Property(x => x.DevelopmentLevel)
                       .HasConversion<int>()
                       .HasColumnName("GenerationDevelopmentLevel")
                       .IsRequired();

                    profile.Property(x => x.EconomyProfile)
                       .HasConversion<int>()
                       .HasColumnName("GenerationEconomyProfile")
                       .IsRequired();

                    profile.Property(x => x.PopulationOccupancyProfile)
                       .HasConversion<int>()
                       .HasColumnName("GenerationPopulationOccupancyProfile")
                       .IsRequired();

                    profile.Property(x => x.PlannedPeopleCount)
                       .HasColumnName("GenerationPlannedPeopleCount")
                       .IsRequired(false);
                });

            builder.Navigation(x => x.GenerationProfile)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.InitialWeatherProfile,
                buildAction: weather =>
                {
                    weather.Property(x => x.Mode)
                       .HasConversion<int>()
                       .HasColumnName("InitialWeatherMode")
                       .IsRequired();

                    weather.Property(x => x.ManualType)
                       .HasConversion<int?>()
                       .HasColumnName("InitialWeatherManualType")
                       .IsRequired(false);

                    weather.Property(x => x.ManualSeverity)
                       .HasConversion<int?>()
                       .HasColumnName("InitialWeatherManualSeverity")
                       .IsRequired(false);

                    weather.Property(x => x.ManualTemperature)
                       .HasConversion(
                            convertToProviderExpression: x => x.HasValue
                                ? x.Value.Value
                                : (decimal?)null,
                            convertFromProviderExpression: x => x.HasValue
                                ? TemperatureC.From(x.Value)
                                : null)
                       .HasColumnName("InitialWeatherManualTemperatureC")
                       .IsRequired(false);
                });

            builder.Navigation(x => x.InitialWeatherProfile)
               .IsRequired();

            builder.Property(x => x.Status)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Property(x => x.ProvisioningCorrelationId)
               .IsRequired(false);

            builder.Property(x => x.PopulationBootstrapOperationId)
               .IsRequired();

            builder.Property(x => x.EconomyBootstrapOperationId)
               .IsRequired();

            builder.Property(x => x.PopulationBootstrapCompletedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.EconomyBootstrapCompletedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.PopulationBootstrapFailedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.EconomyBootstrapFailedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.PopulationBootstrapFailureCode)
               .HasMaxLength(City.PopulationBootstrapFailureCodeMaxLength)
               .IsRequired(false);

            builder.Property(x => x.EconomyBootstrapFailureCode)
               .HasMaxLength(City.EconomyBootstrapFailureCodeMaxLength)
               .IsRequired(false);

            builder.Property(x => x.ArchivedAtUtc)
               .IsRequired(false);

            builder.Ignore(x => x.DomainEvents);

            // Optimizations for common queries
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);
            builder.HasIndex(x => x.ProvisioningCorrelationId)
               .IsUnique()
               .HasFilter("\"ProvisioningCorrelationId\" IS NOT NULL");

            // Postgres optimistic concurrency
            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}

using Matrix.CityCore.Domain.Cities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.CityCore.Infrastructure.Persistence.Configurations
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
                });

            builder.Navigation(x => x.GenerationProfile)
               .IsRequired();

            builder.Property(x => x.Status)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Property(x => x.PopulationBootstrapCompletedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.PopulationBootstrapFailedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.PopulationBootstrapError)
               .HasMaxLength(City.PopulationBootstrapErrorMaxLength)
               .IsRequired(false);

            builder.Property(x => x.ArchivedAtUtc)
               .IsRequired(false);

            builder.Ignore(x => x.DomainEvents);

            // Optimizations for common queries
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);

            // Postgres optimistic concurrency
            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}

using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationActivityEventConfiguration
        : IEntityTypeConfiguration<CityPopulationActivityEvent>
    {
        public void Configure(EntityTypeBuilder<CityPopulationActivityEvent> builder)
        {
            builder.ToTable("CityPopulationActivityEvents");

            builder.HasKey(x => x.ActivityEventId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.CurrentDate)
               .HasConversion(
                    convertToProviderExpression: date => date.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .IsRequired();

            builder.Property(x => x.EventType)
               .HasConversion<string>()
               .HasMaxLength(64)
               .IsRequired();

            builder.Property(x => x.Source)
               .HasConversion<string>()
               .HasMaxLength(32)
               .IsRequired();

            builder.Property(x => x.Severity)
               .HasConversion<string>()
               .HasMaxLength(32)
               .IsRequired();

            builder.Property(x => x.Title)
               .HasMaxLength(240)
               .IsRequired();

            builder.Property(x => x.Summary)
               .HasMaxLength(640)
               .IsRequired();

            builder.Property(x => x.PrimaryResidentId)
               .HasConversion(
                    convertToProviderExpression: id => id == null
                        ? (Guid?)null
                        : id.Value.Value,
                    convertFromProviderExpression: value => value.HasValue
                        ? PersonId.From(value.Value)
                        : null);

            builder.Property(x => x.SecondaryResidentId)
               .HasConversion(
                    convertToProviderExpression: id => id == null
                        ? (Guid?)null
                        : id.Value.Value,
                    convertFromProviderExpression: value => value.HasValue
                        ? PersonId.From(value.Value)
                        : null);

            builder.HasIndex(x => new
            {
                x.CityId,
                x.OccurredAtUtc
            });
            builder.HasIndex(x => new
            {
                x.CityId,
                x.CurrentDate
            });
            builder.HasIndex(x => x.EventType);
        }
    }
}

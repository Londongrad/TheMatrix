using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class CityPopulationProgressionStateConfiguration : IEntityTypeConfiguration<CityPopulationProgressionState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationProgressionState> builder)
        {
            builder.ToTable("CityPopulationProgressionStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.LastProcessedTickId)
               .IsRequired();

            builder.Property(x => x.LastProcessedDate)
               .HasConversion(
                    convertToProviderExpression: date => date.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}

using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.CityCore.Infrastructure.Persistence.Configurations
{
    public sealed class SimulationClockConfiguration : IEntityTypeConfiguration<SimulationClock>
    {
        public void Configure(EntityTypeBuilder<SimulationClock> builder)
        {
            builder.ToTable("SimulationClocks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.CurrentTime)
               .HasConversion(
                    convertToProviderExpression: x => x.ValueUtc,
                    convertFromProviderExpression: x => SimTime.FromUtc(x))
               .IsRequired();

            builder.Property(x => x.TickId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new TickId(x))
               .IsRequired();

            builder.Property(x => x.Speed)
               .HasConversion(
                    convertToProviderExpression: x => x.Multiplier,
                    convertFromProviderExpression: x => SimSpeed.From(x))
               .HasPrecision(
                    precision: 20,
                    scale: 6)
               .IsRequired();

            builder.Property(x => x.State)
               .HasConversion<int>()
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}

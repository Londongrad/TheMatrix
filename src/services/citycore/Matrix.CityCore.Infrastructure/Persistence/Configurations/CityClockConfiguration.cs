using Matrix.CityCore.Domain.Aggregates;
using Matrix.CityCore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.CityCore.Infrastructure.Persistence.Configurations
{
    public class CityClockConfiguration : IEntityTypeConfiguration<CityClock>
    {
        public void Configure(EntityTypeBuilder<CityClock> builder)
        {
            builder.ToTable("city_clock");

            builder.HasKey(c => c.Id);

            builder
               .Property(c => c.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new CityClockId(value))
               .HasColumnName("id");

            builder
               .Property(c => c.SimMinutesPerTick)
               .HasColumnName("sim_minutes_per_tick");

            builder
               .Property(c => c.IsPaused)
               .HasColumnName("is_paused");

            // SimulationTime.Value <-> DateTime
            builder
               .Property(c => c.Time)
               .HasConversion(
                    convertToProviderExpression: t => t.Current,
                    convertFromProviderExpression: dt => new SimulationTime(dt))
               .HasColumnName("current_time_utc");
        }
    }
}

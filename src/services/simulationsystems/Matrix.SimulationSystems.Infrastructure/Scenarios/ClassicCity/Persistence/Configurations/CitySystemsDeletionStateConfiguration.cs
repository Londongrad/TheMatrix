using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class CitySystemsDeletionStateConfiguration : IEntityTypeConfiguration<CitySystemsDeletionState>
    {
        public void Configure(EntityTypeBuilder<CitySystemsDeletionState> builder)
        {
            builder.ToTable("CitySystemsDeletionStates");
            builder.HasKey(x => x.CityId);
            builder.Property(x => x.DeletedAtUtc)
               .IsRequired();
            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();
            builder.HasIndex(x => x.DeletedAtUtc);
        }
    }
}

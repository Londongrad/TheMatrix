using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity;

public sealed class CityHealthcarePressureSnapshotEntityConfiguration
    : IEntityTypeConfiguration<CityHealthcarePressureSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<CityHealthcarePressureSnapshotEntity> builder)
    {
        builder.ToTable("CityHealthcarePressureSnapshots");
        builder.HasKey(snapshot => snapshot.CityId);
        builder.Property(snapshot => snapshot.CurrentDate).HasColumnType("date");
        builder.Property(snapshot => snapshot.MedicalLoadIndex).HasPrecision(8, 4);
        builder.Property(snapshot => snapshot.TriagePressureIndex).HasPrecision(8, 4);
        builder.Property(snapshot => snapshot.RecoverySupportIndex).HasPrecision(8, 4);
        builder.HasIndex(snapshot => snapshot.SourceRevision);
        builder.HasIndex(snapshot => snapshot.UpdatedAtUtc);
    }
}

using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class EducationParticipationProjectionEntityConfiguration
        : IEntityTypeConfiguration<EducationParticipationProjectionEntity>
    {
        public void Configure(EntityTypeBuilder<EducationParticipationProjectionEntity> builder)
        {
            builder.ToTable("EducationParticipationProjections");
            builder.HasKey(projection => new
            {
                projection.SimulationHostId,
                projection.ResidentId
            });

            builder.Property(projection => projection.ParticipationRevision)
               .IsConcurrencyToken()
               .IsRequired();
            builder.Property(projection => projection.ResidentId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => PersonId.From(value))
               .IsRequired();
            builder.Property(projection => projection.ResidentLifecycleRevision)
               .IsRequired();
            builder.Property(projection => projection.ActiveStage)
               .HasMaxLength(64);
            builder.Property(projection => projection.CompletedStage)
               .HasMaxLength(64);
            builder.Property(projection => projection.EnrolledOn)
               .HasColumnType("date");
            builder.Property(projection => projection.CompletedStageOn)
               .HasColumnType("date");
            builder.Property(projection => projection.SnapshotDate)
               .HasColumnType("date")
               .IsRequired();
            builder.Property(projection => projection.OccurredAtUtc)
               .IsRequired();
            builder.Property(projection => projection.UpdatedAtUtc)
               .IsRequired();

            builder.HasOne<Person>()
               .WithMany()
               .HasForeignKey(projection => projection.ResidentId)
               .HasPrincipalKey(person => person.Id)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(projection => new
                {
                    projection.SimulationHostId,
                    projection.IsEnrolled
                });
            builder.HasIndex(projection => new
                {
                    projection.SimulationHostId,
                    projection.InstitutionId
                });
        }
    }
}

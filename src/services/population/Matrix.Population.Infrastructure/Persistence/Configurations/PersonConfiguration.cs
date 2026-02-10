using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Persons");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => PersonId.From(value));

            builder.Property(p => p.HouseholdId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => HouseholdId.From(value))
               .IsRequired();

            builder.HasIndex(p => p.HouseholdId);

            builder.HasOne<Household>()
               .WithMany()
               .HasForeignKey(p => p.HouseholdId)
               .HasPrincipalKey(h => h.Id)
               .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.Sex)
               .HasConversion<string>()
               .IsRequired();

            builder.Property(p => p.Happiness)
               .HasConversion(
                    convertToProviderExpression: h => h.Value,
                    convertFromProviderExpression: v => HappinessLevel.From(v))
               .HasColumnName("Happiness")
               .IsRequired();

            builder.Property(p => p.Energy)
               .HasConversion(
                    convertToProviderExpression: value => value.Value,
                    convertFromProviderExpression: value => EnergyLevel.From(value))
               .HasColumnName("Energy")
               .IsRequired();

            builder.Property(p => p.Stress)
               .HasConversion(
                    convertToProviderExpression: value => value.Value,
                    convertFromProviderExpression: value => StressLevel.From(value))
               .HasColumnName("Stress")
               .IsRequired();

            builder.Property(p => p.SocialNeed)
               .HasConversion(
                    convertToProviderExpression: value => value.Value,
                    convertFromProviderExpression: value => SocialNeedLevel.From(value))
               .HasColumnName("SocialNeed")
               .IsRequired();

            builder.Property(p => p.Weight)
               .HasConversion(
                    convertToProviderExpression: w => w.Kilograms,
                    convertFromProviderExpression: kg => new BodyWeight(kg))
               .HasColumnName("WeightKg")
               .HasColumnType("decimal(5,2)");

            builder.OwnsOne(
                navigationExpression: p => p.Name,
                buildAction: name =>
                {
                    name.Property(n => n.FirstName)
                       .HasColumnName("FirstName")
                       .IsRequired();

                    name.Property(n => n.LastName)
                       .HasColumnName("LastName")
                       .IsRequired();

                    name.Property(n => n.Patronymic)
                       .HasColumnName("MiddleName");
                });

            builder.OwnsOne(
                navigationExpression: p => p.Life,
                buildAction: life =>
                {
                    life.Property(l => l.Status)
                       .HasConversion<string>()
                       .HasColumnName("LifeStatus")
                       .IsRequired();

                    life.Property(l => l.Health)
                       .HasConversion(
                            convertToProviderExpression: h => h.Value,
                            convertFromProviderExpression: v => HealthLevel.From(v))
                       .HasColumnName("Health")
                       .IsRequired();

                    life.OwnsOne(
                        navigationExpression: l => l.Span,
                        buildAction: span =>
                        {
                            span.Property(s => s.BirthDate)
                               .HasConversion(
                                    convertToProviderExpression: d => d.ToDateTime(TimeOnly.MinValue),
                                    convertFromProviderExpression: dt => DateOnly.FromDateTime(dt))
                               .HasColumnName("BirthDate")
                               .HasColumnType("date")
                               .IsRequired();

                            span.Property(s => s.DeathDate)
                               .HasConversion(
                                    convertToProviderExpression: d => d.HasValue
                                        ? d.Value.ToDateTime(TimeOnly.MinValue)
                                        : (DateTime?)null,
                                    convertFromProviderExpression: dt => dt.HasValue
                                        ? DateOnly.FromDateTime(dt.Value)
                                        : null)
                               .HasColumnName("DeathDate")
                               .HasColumnType("date");
                        });
                });

            builder.Ignore(p => p.IsAlive);
            builder.Ignore(p => p.LifeStatus);
            builder.Ignore(p => p.BirthDate);
            builder.Ignore(p => p.DeathDate);
            builder.Ignore(p => p.Health);
            builder.Ignore(p => p.MaritalStatus);
            builder.Ignore(p => p.SpouseId);
            builder.Ignore(p => p.EducationLevel);

            builder.OwnsOne(
                navigationExpression: p => p.Education,
                buildAction: edu =>
                {
                    edu.Property(e => e.Level)
                       .HasConversion<string>()
                       .HasColumnName("EducationLevel")
                       .IsRequired();
                });

            builder.OwnsOne(
                navigationExpression: p => p.Marital,
                buildAction: marital =>
                {
                    marital.Property(m => m.Status)
                       .HasConversion<string>()
                       .HasColumnName("MaritalStatus")
                       .IsRequired();

                    marital.Property(m => m.SpouseId)
                       .HasConversion(
                            convertToProviderExpression: id => id.HasValue
                                ? id.Value.Value
                                : (Guid?)null,
                            convertFromProviderExpression: value => value.HasValue
                                ? PersonId.From(value.Value)
                                : null)
                       .HasColumnName("SpouseId");
                });

            builder.OwnsOne(
                navigationExpression: p => p.Employment,
                buildAction: employment =>
                {
                    employment.Property(e => e.Status)
                       .HasConversion<string>()
                       .HasColumnName("EmploymentStatus")
                       .IsRequired();

                    employment.OwnsOne(
                        navigationExpression: e => e.Job,
                        buildAction: job =>
                        {
                            job.Property(j => j.WorkplaceId)
                               .HasConversion(
                                    convertToProviderExpression: id => id.Value,
                                    convertFromProviderExpression: value => WorkplaceId.From(value))
                               .HasColumnName("WorkplaceId");

                            job.Property(j => j.Title)
                               .HasColumnName("JobTitle")
                               .HasMaxLength(200);
                        });
                });

            builder.OwnsOne(
                navigationExpression: p => p.Personality,
                buildAction: personality =>
                {
                    personality.Property(x => x.Optimism)
                       .HasColumnName("Optimism")
                       .IsRequired();

                    personality.Property(x => x.Discipline)
                       .HasColumnName("Discipline")
                       .IsRequired();

                    personality.Property(x => x.RiskTolerance)
                       .HasColumnName("RiskTolerance")
                       .IsRequired();

                    personality.Property(x => x.Sociability)
                       .HasColumnName("Sociability")
                       .IsRequired();
                });
        }
    }
}

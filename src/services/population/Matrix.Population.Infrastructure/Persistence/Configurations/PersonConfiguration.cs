using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Persons");

            // Primary key
            builder.HasKey(p => p.Id);

            // ValueObject PersonId <-> Guid
            builder
                .Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => new PersonId(value))
                .HasColumnName("id");

            builder
                .Property(p => p.HouseholdId)
                .HasConversion(
                    id => id.Value,
                    value => new HouseholdId(value))
                .HasColumnName("household_id");

            builder
                .Property(p => p.DistrictId)
                .HasConversion(
                    id => id.Value,
                    value => new DistrictId(value))
                .HasColumnName("district_id");

            // Age (ValueObject) <-> int (years)
            builder
                .Property(p => p.Age)
                .HasConversion(
                    age => age.Years,
                    years => new Age(years))
                .HasColumnName("age_years");

            // EmploymentStatus enum как int
            builder
                .Property(p => p.EmploymentStatus)
                .HasConversion<int>()
                .HasColumnName("employment_status");

            // Job как "owned entity"
            builder.OwnsOne(p => p.Job, job =>
            {
                job.Property(j => j.WorkplaceId)
                    .HasConversion(
                        id => id.Value,
                        value => new WorkplaceId(value))
                    .HasColumnName("job_workplace_id");

                job.Property(j => j.GrossMonthlySalary)
                    .HasColumnName("job_gross_monthly_salary");

                job.Property(j => j.IncomeTaxRate)
                    .HasColumnName("job_income_tax_rate");
            });

            // Можно добавить индексы
            builder
                .HasIndex(p => p.DistrictId)
                .HasDatabaseName("ix_persons_district_id");

            builder
                .HasIndex(p => p.HouseholdId)
                .HasDatabaseName("ix_persons_household_id");
        }
    }
}

using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class ResidentPlacementPoolBuilderTests
{
    private static readonly DateOnly CurrentDate = new(2048, 5, 6);

    [Fact]
    public void BuildEducationInstitutionPools_WhenPersonsHaveNoInstitutions_ReturnsEmpty()
    {
        Person[] persons =
        [
            CreatePerson(personId: Guid.NewGuid()),
            CreatePerson(personId: Guid.NewGuid())
        ];

        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> pools =
            ResidentPlacementPoolBuilder.BuildEducationInstitutionPools(persons);

        Assert.Empty(pools);
    }

    [Fact]
    public void BuildEducationInstitutionPools_GroupsInstitutionsByEducationLevel()
    {
        EducationInstitutionId upperInstitution = EducationInstitutionId.From(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));
        EducationInstitutionId higherInstitution = EducationInstitutionId.From(
            Guid.Parse("22222222-2222-2222-2222-222222222222"));
        Person upperStudent = CreateStudent(
            level: EducationLevel.UpperSecondary,
            institutionId: upperInstitution);
        Person higherStudent = CreateStudent(
            level: EducationLevel.Higher,
            institutionId: higherInstitution);

        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> pools =
            ResidentPlacementPoolBuilder.BuildEducationInstitutionPools([upperStudent, higherStudent]);

        Assert.Equal(2, pools.Count);
        Assert.Equal(upperInstitution, Assert.Single(pools[EducationLevel.UpperSecondary]).InstitutionId);
        Assert.Equal(higherInstitution, Assert.Single(pools[EducationLevel.Higher]).InstitutionId);
    }

    [Fact]
    public void BuildEducationInstitutionPools_DeduplicatesInstitutionPerLevelAndKeepsFirstAnchor()
    {
        EducationInstitutionId institutionId = EducationInstitutionId.From(
            Guid.Parse("33333333-3333-3333-3333-333333333333"));
        CityAnchorId firstAnchorId = CityAnchorId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        CityAnchorId secondAnchorId = CityAnchorId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        Person firstStudent = CreateStudent(
            level: EducationLevel.UpperSecondary,
            institutionId: institutionId,
            institutionAnchorId: firstAnchorId);
        Person duplicateStudent = CreateStudent(
            level: EducationLevel.UpperSecondary,
            institutionId: institutionId,
            institutionAnchorId: secondAnchorId);

        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> pools =
            ResidentPlacementPoolBuilder.BuildEducationInstitutionPools([firstStudent, duplicateStudent]);

        CityEducationInstitutionBinding binding = Assert.Single(pools[EducationLevel.UpperSecondary]);
        Assert.Equal(institutionId, binding.InstitutionId);
        Assert.Equal(firstAnchorId, binding.InstitutionAnchorId);
    }

    [Fact]
    public void BuildEducationInstitutionPools_AllowsSameInstitutionAcrossDifferentLevels()
    {
        EducationInstitutionId institutionId = EducationInstitutionId.From(
            Guid.Parse("44444444-4444-4444-4444-444444444444"));
        Person upperStudent = CreateStudent(
            level: EducationLevel.UpperSecondary,
            institutionId: institutionId);
        Person higherStudent = CreateStudent(
            level: EducationLevel.Higher,
            institutionId: institutionId);

        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> pools =
            ResidentPlacementPoolBuilder.BuildEducationInstitutionPools([upperStudent, higherStudent]);

        Assert.Equal(institutionId, Assert.Single(pools[EducationLevel.UpperSecondary]).InstitutionId);
        Assert.Equal(institutionId, Assert.Single(pools[EducationLevel.Higher]).InstitutionId);
    }

    [Fact]
    public void BuildWorkplacePools_WhenPersonsAreNotEmployed_ReturnsEmpty()
    {
        Person unemployed = CreatePerson(personId: Guid.NewGuid());
        Person student = CreateStudent(
            level: EducationLevel.UpperSecondary,
            institutionId: EducationInstitutionId.From(Guid.Parse("55555555-5555-5555-5555-555555555555")));
        Person retired = CreatePerson(
            personId: Guid.NewGuid(),
            birthDate: new DateOnly(1940, 1, 1),
            currentDate: CurrentDate);
        retired.Retire(CurrentDate);

        Dictionary<string, List<Job>> pools =
            ResidentPlacementPoolBuilder.BuildWorkplacePools([unemployed, student, retired]);

        Assert.Empty(pools);
    }

    [Fact]
    public void BuildWorkplacePools_GroupsJobsByTitleCaseInsensitively()
    {
        WorkplaceId firstWorkplaceId = WorkplaceId.From(Guid.Parse("66666666-6666-6666-6666-666666666666"));
        WorkplaceId secondWorkplaceId = WorkplaceId.From(Guid.Parse("77777777-7777-7777-7777-777777777777"));
        Person firstEngineer = CreateEmployedPerson(CreateJob(firstWorkplaceId, "Engineer"));
        Person secondEngineer = CreateEmployedPerson(CreateJob(secondWorkplaceId, "engineer"));

        Dictionary<string, List<Job>> pools =
            ResidentPlacementPoolBuilder.BuildWorkplacePools([firstEngineer, secondEngineer]);

        Assert.Single(pools);
        Assert.True(pools.ContainsKey("ENGINEER"));
        Assert.Equal(
            [firstWorkplaceId, secondWorkplaceId],
            pools["ENGINEER"].Select(job => job.WorkplaceId).ToArray());
    }

    [Fact]
    public void BuildWorkplacePools_DeduplicatesWorkplacePerTitleAndKeepsFirstJob()
    {
        WorkplaceId workplaceId = WorkplaceId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        CityAnchorId firstAnchorId = CityAnchorId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        CityAnchorId secondAnchorId = CityAnchorId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        Person firstEngineer = CreateEmployedPerson(CreateJob(
            workplaceId: workplaceId,
            title: "Engineer",
            workplaceAnchorId: firstAnchorId));
        Person duplicateEngineer = CreateEmployedPerson(CreateJob(
            workplaceId: workplaceId,
            title: "Engineer",
            workplaceAnchorId: secondAnchorId));

        Dictionary<string, List<Job>> pools =
            ResidentPlacementPoolBuilder.BuildWorkplacePools([firstEngineer, duplicateEngineer]);

        Job job = Assert.Single(pools["Engineer"]);
        Assert.Equal(workplaceId, job.WorkplaceId);
        Assert.Equal(firstAnchorId, job.WorkplaceAnchorId);
    }

    [Fact]
    public void BuildWorkplacePools_AllowsSameWorkplaceAcrossDifferentTitles()
    {
        WorkplaceId workplaceId = WorkplaceId.From(Guid.Parse("99999999-9999-9999-9999-999999999999"));
        Person engineer = CreateEmployedPerson(CreateJob(workplaceId, "Engineer"));
        Person doctor = CreateEmployedPerson(CreateJob(workplaceId, "Doctor"));

        Dictionary<string, List<Job>> pools =
            ResidentPlacementPoolBuilder.BuildWorkplacePools([engineer, doctor]);

        Assert.Equal(workplaceId, Assert.Single(pools["Engineer"]).WorkplaceId);
        Assert.Equal(workplaceId, Assert.Single(pools["Doctor"]).WorkplaceId);
    }

    private static Person CreateStudent(
        EducationLevel level,
        EducationInstitutionId institutionId,
        CityAnchorId? institutionAnchorId = null)
    {
        Person person = CreatePerson(personId: Guid.NewGuid());

        if (person.EducationLevel != level)
            person.GraduateTo(level);

        person.StartStudying(
            currentDate: CurrentDate,
            institutionId: institutionId,
            institutionAnchorId: institutionAnchorId);

        return person;
    }

    private static Person CreateEmployedPerson(Job job)
    {
        return CreatePerson(
            personId: Guid.NewGuid(),
            employmentStatus: EmploymentStatus.Employed,
            job: job);
    }

    private static Job CreateJob(
        WorkplaceId workplaceId,
        string title,
        CityAnchorId? workplaceAnchorId = null)
    {
        return new Job(
            workplaceId: workplaceId,
            title: title,
            workplaceAnchorId: workplaceAnchorId);
    }
}

using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class ResidentProgressionActivityCollectorTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly DateOnly CurrentDate = new(2048, 5, 6);
    private static readonly DateTimeOffset OccurredAtUtc = new(2048, 5, 6, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Collect_WhenResidentDidNotChange_AddsNoEntries()
    {
        Person resident = CreatePerson();

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: _ => { });

        Assert.Empty(entries);
    }

    [Fact]
    public void Collect_WhenResidentDies_AddsResidentDiedEvent()
    {
        Person resident = CreatePerson();

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.Die(CurrentDate));

        CityPopulationActivityWriteModel entry = AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentDied,
            resident: resident);
        Assert.Equal(CityPopulationActivitySeverity.Danger, entry.Severity);
    }

    [Fact]
    public void Collect_WhenResidentBecomesWidowed_AddsWidowedEventWithSpouseName()
    {
        PersonId spouseId = PersonId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        Person spouse = CreatePerson(
            personId: spouseId.Value,
            firstName: "Trinity",
            lastName: "Matrix");
        Person resident = CreatePerson(
            maritalStatus: MaritalStatus.Married,
            spouseId: spouse.Id);

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.BecomeWidowed(),
            residentsById: new Dictionary<PersonId, Person>
            {
                [resident.Id] = resident,
                [spouse.Id] = spouse
            });

        CityPopulationActivityWriteModel entry = AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentBecameWidowed,
            resident: resident);
        Assert.Contains("Trinity", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Collect_WhenEducationLevelIncreases_AddsGraduatedEvent()
    {
        Person resident = CreatePerson();

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.GraduateTo(EducationLevel.Higher));

        AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentGraduated,
            resident: resident);
    }

    [Fact]
    public void Collect_WhenResidentBecomesIll_AddsBecameIllEvent()
    {
        Person resident = CreatePerson();

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Mild,
                currentDate: CurrentDate));

        AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentBecameIll,
            resident: resident);
    }

    [Fact]
    public void Collect_WhenResidentRecoversFromIllness_AddsRecoveredEvent()
    {
        Person resident = CreatePerson();
        resident.DiagnoseIllness(
            kind: IllnessKind.Exposure,
            severity: IllnessSeverity.Moderate,
            currentDate: CurrentDate.AddDays(-1));

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.RecoverFromIllness(CurrentDate));

        CityPopulationActivityWriteModel entry = AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentRecoveredFromIllness,
            resident: resident);
        Assert.Contains("exposure", entry.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Collect_WhenResidentStartsStudying_AddsEnrolledEvent()
    {
        Person resident = CreatePerson();

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.StartStudying(
                currentDate: CurrentDate,
                institutionId: EducationInstitutionId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"))));

        AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentEnrolled,
            resident: resident);
    }

    [Fact]
    public void Collect_WhenResidentStopsStudying_AddsWithdrewFromStudyEvent()
    {
        Person resident = CreatePerson();
        resident.StartStudying(
            currentDate: CurrentDate.AddDays(-1),
            institutionId: EducationInstitutionId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")));

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.StopStudying(CurrentDate));

        AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentWithdrewFromStudy,
            resident: resident);
    }

    [Fact]
    public void Collect_WhenResidentStartsWorking_AddsHiredEvent()
    {
        Person resident = CreatePerson();

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.AssignJob(
                currentDate: CurrentDate,
                job: CreateJob("Engineer")));

        AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentHired,
            resident: resident);
    }

    [Fact]
    public void Collect_WhenResidentIsFired_AddsFiredEvent()
    {
        Person resident = CreatePerson(
            employmentStatus: EmploymentStatus.Employed,
            job: CreateJob("Operator"));

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.Fire(CurrentDate));

        CityPopulationActivityWriteModel entry = AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentFired,
            resident: resident);
        Assert.Contains("Operator", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Collect_WhenResidentRetires_AddsRetiredEvent()
    {
        Person resident = CreatePerson(
            birthDate: new DateOnly(1940, 1, 1),
            currentDate: CurrentDate);

        IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
            resident: resident,
            mutate: person => person.Retire(CurrentDate));

        AssertSingleEvent(
            entries: entries,
            eventType: CityPopulationActivityEventType.ResidentRetired,
            resident: resident);
    }

    private static IReadOnlyList<CityPopulationActivityWriteModel> CollectAfter(
        Person resident,
        Action<Person> mutate,
        IReadOnlyDictionary<PersonId, Person>? residentsById = null)
    {
        ResidentProgressionActivityCollector.Snapshot before =
            ResidentProgressionActivityCollector.Capture(resident);

        mutate(resident);

        var entries = new List<CityPopulationActivityWriteModel>();

        ResidentProgressionActivityCollector.Collect(
            cityId: TestCityId,
            currentDate: CurrentDate,
            before: before,
            resident: resident,
            residentsById: residentsById ?? new Dictionary<PersonId, Person>
            {
                [resident.Id] = resident
            },
            activityEntries: entries,
            occurredAtUtc: OccurredAtUtc);

        return entries;
    }

    private static CityPopulationActivityWriteModel AssertSingleEvent(
        IReadOnlyList<CityPopulationActivityWriteModel> entries,
        CityPopulationActivityEventType eventType,
        Person resident)
    {
        CityPopulationActivityWriteModel entry = Assert.Single(entries);
        Assert.Equal(TestCityId.Value, entry.CityId);
        Assert.Equal(CurrentDate, entry.CurrentDate);
        Assert.Equal(OccurredAtUtc, entry.OccurredAtUtc);
        Assert.Equal(eventType, entry.EventType);
        Assert.Equal(CityPopulationActivitySource.Autonomy, entry.Source);
        Assert.Equal(resident.Id.Value, entry.PrimaryResidentId);

        return entry;
    }

    private static Job CreateJob(string title)
    {
        return new Job(
            workplaceId: WorkplaceId.From(Guid.NewGuid()),
            title: title);
    }
}

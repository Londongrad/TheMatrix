using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ResidentProgressionActivityCollectorTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 6);

        private static readonly DateTimeOffset OccurredAtUtc = new(
            year: 2048,
            month: 5,
            day: 6,
            hour: 12,
            minute: 30,
            second: 0,
            offset: TimeSpan.Zero);

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
            Assert.Equal(
                expected: CityPopulationActivitySeverity.Danger,
                actual: entry.Severity);
        }

        [Fact]
        public void Collect_WhenResidentBecomesWidowed_AddsWidowedEventWithSpouseName()
        {
            var spouseId = PersonId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
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
            Assert.Contains(
                expectedSubstring: "Trinity",
                actualString: entry.Summary,
                comparisonType: StringComparison.Ordinal);
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
                mutate: person => ApplyHealthcareProjection(
                    person: person,
                    currentDate: CurrentDate,
                    illnessKind: IllnessKind.Infection,
                    illnessSeverity: IllnessSeverity.Mild,
                    diagnosedOn: CurrentDate));

            AssertSingleEvent(
                entries: entries,
                eventType: CityPopulationActivityEventType.ResidentBecameIll,
                resident: resident);
        }

        [Fact]
        public void Collect_WhenResidentRecoversFromIllness_AddsRecoveredEvent()
        {
            Person resident = CreatePerson();
            ApplyHealthcareProjection(
                person: resident,
                currentDate: CurrentDate,
                illnessKind: IllnessKind.Exposure,
                illnessSeverity: IllnessSeverity.Moderate,
                diagnosedOn: CurrentDate.AddDays(-1));

            IReadOnlyList<CityPopulationActivityWriteModel> entries = CollectAfter(
                resident: resident,
                mutate: person => ApplyHealthcareProjection(
                    person: person,
                    currentDate: CurrentDate,
                    illnessKind: null,
                    illnessSeverity: null,
                    lastRecoveredOn: CurrentDate));

            CityPopulationActivityWriteModel entry = AssertSingleEvent(
                entries: entries,
                eventType: CityPopulationActivityEventType.ResidentRecoveredFromIllness,
                resident: resident);
            Assert.Contains(
                expectedSubstring: "exposure",
                actualString: entry.Summary,
                comparisonType: StringComparison.OrdinalIgnoreCase);
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
            Assert.Contains(
                expectedSubstring: "Operator",
                actualString: entry.Summary,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public void Collect_WhenResidentRetires_AddsRetiredEvent()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(
                    year: 1940,
                    month: 1,
                    day: 1),
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
                residentsById: residentsById ??
                               new Dictionary<PersonId, Person>
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
            Assert.Equal(
                expected: TestCityId.Value,
                actual: entry.CityId);
            Assert.Equal(
                expected: CurrentDate,
                actual: entry.CurrentDate);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: entry.OccurredAtUtc);
            Assert.Equal(
                expected: eventType,
                actual: entry.EventType);
            Assert.Equal(
                expected: CityPopulationActivitySource.Autonomy,
                actual: entry.Source);
            Assert.Equal(
                expected: resident.Id.Value,
                actual: entry.PrimaryResidentId);

            return entry;
        }

        private static Job CreateJob(string title)
        {
            return new Job(
                workplaceId: WorkplaceId.From(Guid.NewGuid()),
                title: title);
        }
    }
}

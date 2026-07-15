using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ResidentPlacementPoolBuilderTests
    {
        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 6);

        [Fact]
        public void BuildWorkplacePools_WhenPersonsAreNotEmployed_ReturnsEmpty()
        {
            Person unemployed = CreatePerson(personId: Guid.NewGuid());
            Person retired = CreatePerson(
                personId: Guid.NewGuid(),
                birthDate: new DateOnly(
                    year: 1940,
                    month: 1,
                    day: 1),
                currentDate: CurrentDate);
            retired.Retire(CurrentDate);

            Dictionary<string, List<Job>> pools =
                ResidentPlacementPoolBuilder.BuildWorkplacePools(
                [
                    unemployed,
                    retired
                ]);

            Assert.Empty(pools);
        }

        [Fact]
        public void BuildWorkplacePools_GroupsJobsByTitleCaseInsensitively()
        {
            var firstWorkplaceId = WorkplaceId.From(Guid.Parse("66666666-6666-6666-6666-666666666666"));
            var secondWorkplaceId = WorkplaceId.From(Guid.Parse("77777777-7777-7777-7777-777777777777"));
            Person firstEngineer = CreateEmployedPerson(
                CreateJob(
                    workplaceId: firstWorkplaceId,
                    title: "Engineer"));
            Person secondEngineer = CreateEmployedPerson(
                CreateJob(
                    workplaceId: secondWorkplaceId,
                    title: "engineer"));

            Dictionary<string, List<Job>> pools =
                ResidentPlacementPoolBuilder.BuildWorkplacePools(
                [
                    firstEngineer,
                    secondEngineer
                ]);

            Assert.Single(pools);
            Assert.True(pools.ContainsKey("ENGINEER"));
            Assert.Equal(
                expectedSpan:
                [
                    firstWorkplaceId,
                    secondWorkplaceId
                ],
                actualArray: pools["ENGINEER"]
                   .Select(job => job.WorkplaceId)
                   .ToArray());
        }

        [Fact]
        public void BuildWorkplacePools_DeduplicatesWorkplacePerTitleAndKeepsFirstJob()
        {
            var workplaceId = WorkplaceId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));
            var firstAnchorId = CityAnchorId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
            var secondAnchorId = CityAnchorId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
            Person firstEngineer = CreateEmployedPerson(
                CreateJob(
                    workplaceId: workplaceId,
                    title: "Engineer",
                    workplaceAnchorId: firstAnchorId));
            Person duplicateEngineer = CreateEmployedPerson(
                CreateJob(
                    workplaceId: workplaceId,
                    title: "Engineer",
                    workplaceAnchorId: secondAnchorId));

            Dictionary<string, List<Job>> pools =
                ResidentPlacementPoolBuilder.BuildWorkplacePools(
                [
                    firstEngineer,
                    duplicateEngineer
                ]);

            Job job = Assert.Single(pools["Engineer"]);
            Assert.Equal(
                expected: workplaceId,
                actual: job.WorkplaceId);
            Assert.Equal(
                expected: firstAnchorId,
                actual: job.WorkplaceAnchorId);
        }

        [Fact]
        public void BuildWorkplacePools_AllowsSameWorkplaceAcrossDifferentTitles()
        {
            var workplaceId = WorkplaceId.From(Guid.Parse("99999999-9999-9999-9999-999999999999"));
            Person engineer = CreateEmployedPerson(
                CreateJob(
                    workplaceId: workplaceId,
                    title: "Engineer"));
            Person doctor = CreateEmployedPerson(
                CreateJob(
                    workplaceId: workplaceId,
                    title: "Doctor"));

            Dictionary<string, List<Job>> pools =
                ResidentPlacementPoolBuilder.BuildWorkplacePools(
                [
                    engineer,
                    doctor
                ]);

            Assert.Equal(
                expected: workplaceId,
                actual: Assert.Single(pools["Engineer"])
                   .WorkplaceId);
            Assert.Equal(
                expected: workplaceId,
                actual: Assert.Single(pools["Doctor"])
                   .WorkplaceId);
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
}

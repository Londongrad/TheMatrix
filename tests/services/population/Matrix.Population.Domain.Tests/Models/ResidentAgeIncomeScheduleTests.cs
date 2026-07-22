using Matrix.Population.Domain.Models;
using Xunit;

namespace Matrix.Population.Domain.Tests.Models
{
    public sealed class ResidentAgeIncomeScheduleTests
    {
        [Theory]
        [InlineData(0, 2)]
        [InlineData(16, 2)]
        [InlineData(17, 7)]
        [InlineData(65, 7)]
        [InlineData(66, 12)]
        [InlineData(120, 12)]
        public void Resolve_SelectsInclusiveAgeBand(int age, decimal expected)
        {
            var schedule = ResidentAgeIncomeSchedule.Create((0, 2m), (17, 7m), (66, 12m));

            Assert.Equal(expected, schedule.Resolve(age));
        }

        [Fact]
        public void Create_CopiesInput()
        {
            (int, decimal)[] bands = [(0, 2m), (17, 7m)];
            var schedule = ResidentAgeIncomeSchedule.Create(bands);
            bands[1] = (1, 999m);

            Assert.Equal(7m, schedule.Resolve(17));
        }

        [Fact]
        public void Create_RejectsIncompleteOrAmbiguousSchedules()
        {
            Assert.Throws<ArgumentNullException>(() => ResidentAgeIncomeSchedule.Create(null!));
            Assert.Throws<ArgumentException>(() => ResidentAgeIncomeSchedule.Create());
            Assert.Throws<ArgumentException>(() => ResidentAgeIncomeSchedule.Create((1, 2m)));
            Assert.Throws<ArgumentException>(() => ResidentAgeIncomeSchedule.Create((0, 2m), (0, 3m)));
            Assert.Throws<ArgumentException>(() => ResidentAgeIncomeSchedule.Create((0, 2m), (17, 3m), (10, 4m)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResidentAgeIncomeSchedule.Create((0, -1m)));
        }

        [Fact]
        public void Resolve_RejectsNegativeAge()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ResidentAgeIncomeSchedule.None.Resolve(-1));
        }

        [Fact]
        public void None_ReturnsZeroAtAnyAge()
        {
            Assert.Equal(0m, ResidentAgeIncomeSchedule.None.Resolve(0));
            Assert.Equal(0m, ResidentAgeIncomeSchedule.None.Resolve(int.MaxValue));
        }
    }
}

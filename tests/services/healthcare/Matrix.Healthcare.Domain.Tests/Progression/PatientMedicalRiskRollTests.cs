using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientMedicalRiskRollTests
    {
        private static readonly PatientId PatientId =
            new(Guid.Parse("11111111-2222-3333-4444-555555555555"));

        [Fact]
        public void Occurs_SameInputs_ReturnsDeterministicDecision()
        {
            var roll = new PatientMedicalRiskRoll();
            DateOnly currentDate = new(2048, 5, 6);

            bool first = roll.Occurs(PatientId, currentDate, 401, 0.37d, 2);
            bool second = roll.Occurs(PatientId, currentDate, 401, 0.37d, 2);

            Assert.Equal(first, second);
        }

        [Theory]
        [InlineData(0d, 3, false)]
        [InlineData(1d, 1, true)]
        [InlineData(1d, 7, true)]
        public void Occurs_BoundaryChance_ReturnsExpected(
            double chance,
            int reviewWindows,
            bool expected)
        {
            bool result = new PatientMedicalRiskRoll().Occurs(
                PatientId,
                new DateOnly(2048, 5, 6),
                salt: 503,
                chancePerReview: chance,
                reviewWindows: reviewWindows);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Occurs_MultipleWindows_CompoundDailyChance()
        {
            var roll = new PatientMedicalRiskRoll();
            DateOnly currentDate = new(2048, 5, 6);

            bool oneWindow = roll.Occurs(PatientId, currentDate, 541, 0.2d, 1);
            bool sevenWindows = roll.Occurs(PatientId, currentDate, 541, 0.2d, 7);

            Assert.False(oneWindow && !sevenWindows);
        }
    }
}

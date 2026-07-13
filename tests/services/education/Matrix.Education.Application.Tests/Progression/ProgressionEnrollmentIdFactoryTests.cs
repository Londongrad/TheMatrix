using Matrix.Education.Application.Progression;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Progression
{
    public sealed class ProgressionEnrollmentIdFactoryTests
    {
        [Fact]
        public void Create_ForSameProgressionDecision_IsStable()
        {
            var simulationHostId = new SimulationHostId(
                Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var residentId = new ResidentId(
                Guid.Parse("22222222-2222-2222-2222-222222222222"));
            var stage = new EducationStageKey("primary");
            var enrolledOn = new DateOnly(2048, 9, 1);

            Guid first = ProgressionEnrollmentIdFactory.Create(
                    simulationHostId,
                    residentId,
                    stage,
                    enrolledOn,
                    tickId: 42)
               .Value;
            Guid repeated = ProgressionEnrollmentIdFactory.Create(
                    simulationHostId,
                    residentId,
                    stage,
                    enrolledOn,
                    tickId: 42)
               .Value;

            Assert.Equal(first, repeated);
        }

        [Fact]
        public void Create_ForDifferentTicks_ProducesDifferentIdentifiers()
        {
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            var residentId = new ResidentId(Guid.NewGuid());
            var stage = new EducationStageKey("primary");
            var enrolledOn = new DateOnly(2048, 9, 1);

            Guid first = ProgressionEnrollmentIdFactory.Create(
                    simulationHostId,
                    residentId,
                    stage,
                    enrolledOn,
                    tickId: 42)
               .Value;
            Guid next = ProgressionEnrollmentIdFactory.Create(
                    simulationHostId,
                    residentId,
                    stage,
                    enrolledOn,
                    tickId: 43)
               .Value;

            Assert.NotEqual(first, next);
        }
    }
}

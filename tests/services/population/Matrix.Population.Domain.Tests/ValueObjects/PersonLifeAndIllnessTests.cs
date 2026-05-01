using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.ValueObjects;

public sealed class PersonLifeAndIllnessTests
{
    [Fact]
    public void PersonNameFromFullName_WhenTwoOrThreePartsProvided_ParsesAndFormatsCorrectly()
    {
        PersonName simple = PersonName.FromFullName("Ivanov Ivan");
        PersonName withPatronymic = PersonName.FromFullName("Ivanov Ivan Ivanovich");

        Assert.Equal("Ivan", simple.FirstName);
        Assert.Equal("Ivanov", simple.LastName);
        Assert.Null(simple.Patronymic);
        Assert.Equal("Ivanov Ivan", simple.ToString());

        Assert.Equal("Ivan", withPatronymic.FirstName);
        Assert.Equal("Ivanov", withPatronymic.LastName);
        Assert.Equal("Ivanovich", withPatronymic.Patronymic);
        Assert.Equal("Ivanov Ivan Ivanovich", withPatronymic.ToString());
    }

    [Fact]
    public void PersonNameFromFullName_WhenPartCountIsInvalid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => PersonName.FromFullName("Ivan"));
        Assert.Throws<DomainException>(() => PersonName.FromFullName("A B C D"));
    }

    [Fact]
    public void AgeFromBirthDateAndAddYears_WhenInputsAreValid_ReturnExpectedYears()
    {
        Age age = Age.FromBirthDate(
            birthDate: new DateOnly(2030, 5, 10),
            currentDate: new DateOnly(2048, 5, 9));

        Assert.Equal(17, age.Years);
        Assert.Equal(20, age.AddYears(3).Years);
    }

    [Fact]
    public void LifeStateWithHealthDelta_WhenHealthDropsToZero_MarksPersonAsDeceased()
    {
        LifeState lifeState = LifeState.Create(
            status: LifeStatus.Alive,
            span: LifeSpan.FromBirthDate(new DateOnly(2030, 1, 1)),
            health: HealthLevel.From(10));

        LifeState updated = lifeState.WithHealthDelta(
            delta: -20,
            currentDate: new DateOnly(2048, 5, 1));

        Assert.Equal(LifeStatus.Deceased, updated.Status);
        Assert.Equal(0, updated.Health.Value);
        Assert.Equal(new DateOnly(2048, 5, 1), updated.DeathDate);
    }

    [Fact]
    public void IllnessInfoLifecycle_WhenDiagnosedProgressedAndRecovered_TracksStateTransitions()
    {
        IllnessInfo illness = IllnessInfo.Healthy();

        illness = illness.Diagnose(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Mild,
            currentDate: new DateOnly(2048, 5, 1));
        illness = illness.ProgressTo(IllnessSeverity.Severe);

        Assert.True(illness.HasActiveIllness);
        Assert.Equal(IllnessKind.Infection, illness.CurrentKind);
        Assert.Equal(IllnessSeverity.Severe, illness.CurrentSeverity);
        Assert.Equal(new DateOnly(2048, 5, 1), illness.DiagnosedOn);

        IllnessInfo recovered = illness.Recover(new DateOnly(2048, 5, 4));

        Assert.False(recovered.HasActiveIllness);
        Assert.Null(recovered.CurrentKind);
        Assert.Equal(new DateOnly(2048, 5, 4), recovered.LastRecoveredOn);
    }
}

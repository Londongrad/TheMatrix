using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Care;

public sealed class PatientCareNeedTests
{
    private static readonly PatientId PatientId = new(Guid.NewGuid());
    private static readonly SimulationHostId SimulationHostId = new(Guid.NewGuid());
    private static readonly DateTimeOffset AssessedAtUtc =
        DateTimeOffset.Parse("2048-04-03T10:00:00+00:00");

    [Fact]
    public void Register_CreatesActiveVersionedNeed()
    {
        PatientCareNeed need = CreateNeed();

        Assert.Equal(PatientId, need.PatientId);
        Assert.Equal(SimulationHostId, need.SimulationHostId);
        Assert.Equal(CareNeedUrgency.Urgent, need.Urgency);
        Assert.True(need.IsActive);
        Assert.Equal(new DateOnly(2048, 4, 3), need.RequestedOn);
        Assert.Null(need.ResolvedOn);
        Assert.Equal(7, need.LastAssessmentRevision);
        Assert.Equal(2, need.LastLifecycleRevision);
        Assert.Equal(AssessedAtUtc, need.LastAssessedAtUtc);
    }

    [Fact]
    public void TrySynchronizeAssessment_NewerHealthyAssessment_ResolvesNeed()
    {
        PatientCareNeed need = CreateNeed();
        DateOnly resolvedOn = new(2048, 4, 4);

        bool changed = need.TrySynchronizeAssessment(
            SimulationHostId,
            urgency: null,
            assessmentDate: resolvedOn,
            assessmentRevision: 8,
            lifecycleRevision: 2,
            assessedAtUtc: AssessedAtUtc.AddDays(1));

        Assert.True(changed);
        Assert.False(need.IsActive);
        Assert.Equal(resolvedOn, need.ResolvedOn);
        Assert.Equal(8, need.LastAssessmentRevision);
    }

    [Fact]
    public void TrySynchronizeAssessment_StaleRevision_DoesNotOverwriteNeed()
    {
        PatientCareNeed need = CreateNeed();

        bool changed = need.TrySynchronizeAssessment(
            SimulationHostId,
            CareNeedUrgency.Emergency,
            assessmentDate: new DateOnly(2048, 4, 4),
            assessmentRevision: 7,
            lifecycleRevision: 2,
            assessedAtUtc: AssessedAtUtc.AddDays(1));

        Assert.False(changed);
        Assert.Equal(CareNeedUrgency.Urgent, need.Urgency);
        Assert.True(need.IsActive);
    }

    [Fact]
    public void TrySynchronizeAssessment_NewerLifecycle_ReopensResolvedNeed()
    {
        PatientCareNeed need = CreateNeed();
        need.TrySynchronizeAssessment(
            SimulationHostId,
            urgency: null,
            assessmentDate: new DateOnly(2048, 4, 4),
            assessmentRevision: 8,
            lifecycleRevision: 2,
            assessedAtUtc: AssessedAtUtc.AddDays(1));

        bool changed = need.TrySynchronizeAssessment(
            SimulationHostId,
            CareNeedUrgency.Emergency,
            assessmentDate: new DateOnly(2048, 4, 5),
            assessmentRevision: 1,
            lifecycleRevision: 3,
            assessedAtUtc: AssessedAtUtc.AddDays(2));

        Assert.True(changed);
        Assert.True(need.IsActive);
        Assert.Equal(CareNeedUrgency.Emergency, need.Urgency);
        Assert.Equal(new DateOnly(2048, 4, 5), need.RequestedOn);
        Assert.Null(need.ResolvedOn);
    }

    private static PatientCareNeed CreateNeed()
    {
        return PatientCareNeed.Register(
            PatientId,
            SimulationHostId,
            CareNeedUrgency.Urgent,
            requestedOn: new DateOnly(2048, 4, 3),
            assessmentRevision: 7,
            lifecycleRevision: 2,
            assessedAtUtc: AssessedAtUtc);
    }
}

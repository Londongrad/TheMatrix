using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression;

public sealed class PatientHealthProgressionBatchSetTests
{
    private static readonly SimulationHostId SimulationHostId = new(Guid.NewGuid());
    private static readonly DateTimeOffset ReceivedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
    private static readonly DateOnly CurrentDate = new(2048, 5, 6);

    [Fact]
    public void RegisterBatch_OutOfOrderBatches_CompletesOnlyAfterAllUniqueParts()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: 3,
                batchNumber: 3,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        PatientHealthProgressionBatchRegistrationStatus first = batchSet.RegisterBatch(
            "health-risk:17",
            totalBatches: 3,
            batchNumber: 1,
            currentDate: CurrentDate,
            receivedAtUtc: ReceivedAtUtc.AddSeconds(1));
        PatientHealthProgressionBatchRegistrationStatus completed = batchSet.RegisterBatch(
            "health-risk:17",
            totalBatches: 3,
            batchNumber: 2,
            currentDate: CurrentDate,
            receivedAtUtc: ReceivedAtUtc.AddSeconds(2));

        Assert.Equal(PatientHealthProgressionBatchRegistrationStatus.Accepted, first);
        Assert.Equal(PatientHealthProgressionBatchRegistrationStatus.Completed, completed);
        Assert.Equal(3, batchSet.ReceivedBatchCount);
        Assert.True(batchSet.HasReceivedBatch(1));
        Assert.True(batchSet.HasReceivedBatch(2));
        Assert.True(batchSet.HasReceivedBatch(3));
        Assert.True(batchSet.IsComplete);
        Assert.Equal(ReceivedAtUtc.AddSeconds(2), batchSet.CompletedAtUtc);
    }

    [Fact]
    public void RegisterBatch_DuplicatePart_DoesNotAdvanceOrChangeReceiptTime()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: 2,
                batchNumber: 1,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        PatientHealthProgressionBatchRegistrationStatus status = batchSet.RegisterBatch(
            "health-risk:17",
            totalBatches: 2,
            batchNumber: 1,
            currentDate: CurrentDate,
            receivedAtUtc: ReceivedAtUtc.AddMinutes(1));

        Assert.Equal(PatientHealthProgressionBatchRegistrationStatus.Duplicate, status);
        Assert.Equal(1, batchSet.ReceivedBatchCount);
        Assert.Equal(ReceivedAtUtc, batchSet.LastReceivedAtUtc);
        Assert.False(batchSet.IsComplete);
    }

    [Fact]
    public void Start_SingleBatch_CreatesCompletedSet()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: 1,
                batchNumber: 1,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        Assert.True(batchSet.IsComplete);
        Assert.Equal(ReceivedAtUtc, batchSet.CompletedAtUtc);
        Assert.Equal(1, batchSet.ReceivedBatchCount);
    }

    [Fact]
    public void RegisterBatch_ChangedSetMetadata_ThrowsInvalidOperationException()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: 3,
                batchNumber: 1,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        Assert.Throws<InvalidOperationException>(() => batchSet.RegisterBatch(
            "different",
            totalBatches: 3,
            batchNumber: 2,
            currentDate: CurrentDate,
            receivedAtUtc: ReceivedAtUtc));
        Assert.Throws<InvalidOperationException>(() => batchSet.RegisterBatch(
            "health-risk:17",
            totalBatches: 4,
            batchNumber: 2,
            currentDate: CurrentDate,
            receivedAtUtc: ReceivedAtUtc));

        Assert.Throws<InvalidOperationException>(() => batchSet.RegisterBatch(
            "health-risk:17",
            totalBatches: 3,
            batchNumber: 2,
            currentDate: CurrentDate.AddDays(1),
            receivedAtUtc: ReceivedAtUtc));
    }

    [Fact]
    public void Start_MaximumBatchNumber_TracksLastBit()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: PatientHealthProgressionBatchSet.MaxTotalBatches,
                batchNumber: PatientHealthProgressionBatchSet.MaxTotalBatches,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        Assert.True(batchSet.HasReceivedBatch(PatientHealthProgressionBatchSet.MaxTotalBatches));
        Assert.Equal(1, batchSet.ReceivedBatchCount);
        Assert.False(batchSet.IsComplete);
    }

    [Fact]
    public void RecordCareDeliveryBatch_AggregatesEveryReceivedBatchOnce()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: 2,
                batchNumber: 1,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        batchSet.RecordCareDeliveryBatch(
            processedPatientCount: 10,
            routineCareDeliveryCount: 1,
            urgentCareDeliveryCount: 2,
            acuteCareDeliveryCount: 0,
            emergencyCareDeliveryCount: 0);
        batchSet.RegisterBatch(
            "health-risk:17",
            totalBatches: 2,
            batchNumber: 2,
            currentDate: CurrentDate,
            receivedAtUtc: ReceivedAtUtc.AddSeconds(1));
        batchSet.RecordCareDeliveryBatch(
            processedPatientCount: 8,
            routineCareDeliveryCount: 0,
            urgentCareDeliveryCount: 0,
            acuteCareDeliveryCount: 1,
            emergencyCareDeliveryCount: 1);

        Assert.Equal(2, batchSet.RecordedCareDeliveryBatchCount);
        Assert.Equal(18, batchSet.ProcessedPatientCount);
        Assert.Equal(1, batchSet.RoutineCareDeliveryCount);
        Assert.Equal(2, batchSet.UrgentCareDeliveryCount);
        Assert.Equal(1, batchSet.AcuteCareDeliveryCount);
        Assert.Equal(1, batchSet.EmergencyCareDeliveryCount);
    }

    [Fact]
    public void RecordCareDeliveryBatch_DuplicateOrImpossibleActivity_IsRejected()
    {
        PatientHealthProgressionBatchSet batchSet =
            PatientHealthProgressionBatchSet.Start(
                SimulationHostId,
                sourceRevision: 17,
                correlationId: "health-risk:17",
                totalBatches: 1,
                batchNumber: 1,
                currentDate: CurrentDate,
                receivedAtUtc: ReceivedAtUtc);

        Assert.Throws<ArgumentException>(() => batchSet.RecordCareDeliveryBatch(
            processedPatientCount: 1,
            routineCareDeliveryCount: 2,
            urgentCareDeliveryCount: 0,
            acuteCareDeliveryCount: 0,
            emergencyCareDeliveryCount: 0));

        batchSet.RecordCareDeliveryBatch(
            processedPatientCount: 1,
            routineCareDeliveryCount: 1,
            urgentCareDeliveryCount: 0,
            acuteCareDeliveryCount: 0,
            emergencyCareDeliveryCount: 0);

        Assert.Throws<InvalidOperationException>(() => batchSet.RecordCareDeliveryBatch(
            processedPatientCount: 1,
            routineCareDeliveryCount: 0,
            urgentCareDeliveryCount: 1,
            acuteCareDeliveryCount: 0,
            emergencyCareDeliveryCount: 0));
    }
}

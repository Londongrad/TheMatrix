using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Progression;

public sealed class PatientHealthProgressionBatchSet
{
    public const int MaxCorrelationIdLength = 256;
    public const int MaxTotalBatches = 10_000;

    private byte[] _receivedBatchMap = [];

    private PatientHealthProgressionBatchSet(
        SimulationHostId simulationHostId,
        long sourceRevision,
        string correlationId,
        int totalBatches,
        DateOnly currentDate,
        DateTimeOffset firstReceivedAtUtc)
    {
        SimulationHostId = simulationHostId;
        SourceRevision = EnsureRevision(sourceRevision);
        CorrelationId = EnsureCorrelationId(correlationId);
        TotalBatches = EnsureTotalBatches(totalBatches);
        CurrentDate = currentDate;
        _receivedBatchMap = new byte[(totalBatches + 7) / 8];
        FirstReceivedAtUtc = EnsureUtc(firstReceivedAtUtc);
        LastReceivedAtUtc = FirstReceivedAtUtc;
    }

    private PatientHealthProgressionBatchSet()
    {
        CorrelationId = string.Empty;
    }

    public SimulationHostId SimulationHostId { get; private set; }
    public long SourceRevision { get; private set; }
    public string CorrelationId { get; private set; }
    public int TotalBatches { get; private set; }
    public int ReceivedBatchCount { get; private set; }
    public int RecordedCareDeliveryBatchCount { get; private set; }
    public int ProcessedPatientCount { get; private set; }
    public int RoutineCareDeliveryCount { get; private set; }
    public int UrgentCareDeliveryCount { get; private set; }
    public int AcuteCareDeliveryCount { get; private set; }
    public int EmergencyCareDeliveryCount { get; private set; }
    public DateOnly CurrentDate { get; private set; }
    public bool IsComplete { get; private set; }
    public DateTimeOffset FirstReceivedAtUtc { get; private set; }
    public DateTimeOffset LastReceivedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static PatientHealthProgressionBatchSet Start(
        SimulationHostId simulationHostId,
        long sourceRevision,
        string correlationId,
        int totalBatches,
        int batchNumber,
        DateOnly currentDate,
        DateTimeOffset receivedAtUtc)
    {
        var batchSet = new PatientHealthProgressionBatchSet(
            simulationHostId,
            sourceRevision,
            correlationId,
            totalBatches,
            currentDate,
            receivedAtUtc);
        batchSet.RegisterBatch(
            correlationId,
            totalBatches,
            batchNumber,
            currentDate,
            receivedAtUtc);
        return batchSet;
    }

    public PatientHealthProgressionBatchRegistrationStatus RegisterBatch(
        string correlationId,
        int totalBatches,
        int batchNumber,
        DateOnly currentDate,
        DateTimeOffset receivedAtUtc)
    {
        EnsureMatchingSet(correlationId, totalBatches, currentDate);
        EnsureBatchNumber(batchNumber, TotalBatches);
        DateTimeOffset normalizedReceivedAtUtc = EnsureUtc(receivedAtUtc);

        if (HasReceivedBatch(batchNumber))
            return PatientHealthProgressionBatchRegistrationStatus.Duplicate;

        int zeroBasedBatchNumber = batchNumber - 1;
        int byteIndex = zeroBasedBatchNumber / 8;
        int bitIndex = zeroBasedBatchNumber % 8;
        byte[] updatedMap = (byte[])_receivedBatchMap.Clone();
        updatedMap[byteIndex] |= (byte)(1 << bitIndex);
        _receivedBatchMap = updatedMap;
        ReceivedBatchCount++;
        LastReceivedAtUtc = normalizedReceivedAtUtc;

        if (ReceivedBatchCount != TotalBatches)
            return PatientHealthProgressionBatchRegistrationStatus.Accepted;

        IsComplete = true;
        CompletedAtUtc = normalizedReceivedAtUtc;
        return PatientHealthProgressionBatchRegistrationStatus.Completed;
    }

    public void RecordCareDeliveryBatch(
        int processedPatientCount,
        int routineCareDeliveryCount,
        int urgentCareDeliveryCount,
        int acuteCareDeliveryCount,
        int emergencyCareDeliveryCount)
    {
        if (RecordedCareDeliveryBatchCount >= ReceivedBatchCount)
            throw new InvalidOperationException(
                "Care delivery activity can only be recorded once for each received progression batch.");

        int processed = EnsureCount(processedPatientCount, nameof(processedPatientCount));
        int routine = EnsureCount(routineCareDeliveryCount, nameof(routineCareDeliveryCount));
        int urgent = EnsureCount(urgentCareDeliveryCount, nameof(urgentCareDeliveryCount));
        int acute = EnsureCount(acuteCareDeliveryCount, nameof(acuteCareDeliveryCount));
        int emergency = EnsureCount(emergencyCareDeliveryCount, nameof(emergencyCareDeliveryCount));
        int delivered = checked(routine + urgent + acute + emergency);
        if (delivered > processed)
            throw new ArgumentException(
                "Delivered care count cannot exceed the processed patient count.");

        RecordedCareDeliveryBatchCount++;
        ProcessedPatientCount = checked(ProcessedPatientCount + processed);
        RoutineCareDeliveryCount = checked(RoutineCareDeliveryCount + routine);
        UrgentCareDeliveryCount = checked(UrgentCareDeliveryCount + urgent);
        AcuteCareDeliveryCount = checked(AcuteCareDeliveryCount + acute);
        EmergencyCareDeliveryCount = checked(EmergencyCareDeliveryCount + emergency);
    }

    public bool HasReceivedBatch(int batchNumber)
    {
        EnsureBatchNumber(batchNumber, TotalBatches);
        int zeroBasedBatchNumber = batchNumber - 1;
        int byteIndex = zeroBasedBatchNumber / 8;
        int bitIndex = zeroBasedBatchNumber % 8;
        return (_receivedBatchMap[byteIndex] & (1 << bitIndex)) != 0;
    }

    private void EnsureMatchingSet(
        string correlationId,
        int totalBatches,
        DateOnly currentDate)
    {
        string normalizedCorrelationId = EnsureCorrelationId(correlationId);
        if (!string.Equals(normalizedCorrelationId, CorrelationId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "A health progression revision cannot change its correlation identifier.");
        if (EnsureTotalBatches(totalBatches) != TotalBatches)
            throw new InvalidOperationException(
                "A health progression revision cannot change its total batch count.");
        if (currentDate != CurrentDate)
            throw new InvalidOperationException(
                "A health progression revision cannot change its current date.");
    }

    private static long EnsureRevision(long value)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static string EnsureCorrelationId(string? value)
    {
        string normalized = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException(
                message: "A health progression batch set correlation identifier is required.",
                paramName: nameof(value))
            : value.Trim();

        return normalized.Length <= MaxCorrelationIdLength
            ? normalized
            : throw new ArgumentOutOfRangeException(
                paramName: nameof(value),
                message: $"Health progression correlation identifiers cannot exceed {MaxCorrelationIdLength} characters.");
    }

    private static int EnsureTotalBatches(int value)
    {
        return value is > 0 and <= MaxTotalBatches
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static int EnsureCount(int value, string paramName)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(paramName);
    }

    private static void EnsureBatchNumber(int value, int totalBatches)
    {
        if (value <= 0 || value > totalBatches)
            throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        return value.Offset == TimeSpan.Zero
            ? value
            : throw new ArgumentException(
                message: "Health progression batch receipt timestamps must be expressed in UTC.",
                paramName: nameof(value));
    }
}

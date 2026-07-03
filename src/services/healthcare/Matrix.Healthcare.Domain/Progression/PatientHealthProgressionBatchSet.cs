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
        DateTimeOffset firstReceivedAtUtc)
    {
        SimulationHostId = simulationHostId;
        SourceRevision = EnsureRevision(sourceRevision);
        CorrelationId = EnsureCorrelationId(correlationId);
        TotalBatches = EnsureTotalBatches(totalBatches);
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
        DateTimeOffset receivedAtUtc)
    {
        var batchSet = new PatientHealthProgressionBatchSet(
            simulationHostId,
            sourceRevision,
            correlationId,
            totalBatches,
            receivedAtUtc);
        batchSet.RegisterBatch(
            correlationId,
            totalBatches,
            batchNumber,
            receivedAtUtc);
        return batchSet;
    }

    public PatientHealthProgressionBatchRegistrationStatus RegisterBatch(
        string correlationId,
        int totalBatches,
        int batchNumber,
        DateTimeOffset receivedAtUtc)
    {
        EnsureMatchingSet(correlationId, totalBatches);
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

    public bool HasReceivedBatch(int batchNumber)
    {
        EnsureBatchNumber(batchNumber, TotalBatches);
        int zeroBasedBatchNumber = batchNumber - 1;
        int byteIndex = zeroBasedBatchNumber / 8;
        int bitIndex = zeroBasedBatchNumber % 8;
        return (_receivedBatchMap[byteIndex] & (1 << bitIndex)) != 0;
    }

    private void EnsureMatchingSet(string correlationId, int totalBatches)
    {
        string normalizedCorrelationId = EnsureCorrelationId(correlationId);
        if (!string.Equals(normalizedCorrelationId, CorrelationId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "A health progression revision cannot change its correlation identifier.");
        if (EnsureTotalBatches(totalBatches) != TotalBatches)
            throw new InvalidOperationException(
                "A health progression revision cannot change its total batch count.");
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

using System.Net;
using System.Net.Http.Headers;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Matrix.BuildingBlocks.Infrastructure.Tests.TestSupport;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return utcNow;
    }
}

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public HttpResponseMessage Response { get; init; } = new(HttpStatusCode.OK);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Response);
    }
}

internal sealed class FakeOutboxRepository : IOutboxRepository
{
    public IReadOnlyList<LeasedOutboxMessage> BatchToLease { get; init; } = [];

    public DateTime? LeaseNowUtc { get; private set; }
    public Guid LeaseLockToken { get; private set; }
    public DateTime? LeaseLockedUntilUtc { get; private set; }
    public int LeaseBatchSize { get; private set; }

    public List<(Guid MessageId, Guid LockToken, DateTime ProcessedOnUtc)> Processed { get; } = [];
    public List<(Guid MessageId, Guid LockToken, string Error, DateTime NextAttemptOnUtc)> Failed { get; } = [];
    public List<(DateTime ProcessedBeforeUtc, int BatchSize)> CleanupRequests { get; } = [];

    public Task<IReadOnlyList<LeasedOutboxMessage>> LeaseBatchAsync(
        DateTime nowUtc,
        Guid lockToken,
        DateTime lockedUntilUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        LeaseNowUtc = nowUtc;
        LeaseLockToken = lockToken;
        LeaseLockedUntilUtc = lockedUntilUtc;
        LeaseBatchSize = batchSize;
        return Task.FromResult(BatchToLease);
    }

    public Task MarkProcessedAsync(Guid messageId, Guid lockToken, DateTime processedOnUtc, CancellationToken cancellationToken)
    {
        Processed.Add((messageId, lockToken, processedOnUtc));
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(
        Guid messageId,
        Guid lockToken,
        string error,
        DateTime nextAttemptOnUtc,
        CancellationToken cancellationToken)
    {
        Failed.Add((messageId, lockToken, error, nextAttemptOnUtc));
        return Task.CompletedTask;
    }

    public Task<int> DeleteProcessedBatchAsync(DateTime processedBeforeUtc, int batchSize, CancellationToken cancellationToken)
    {
        CleanupRequests.Add((processedBeforeUtc, batchSize));
        return Task.FromResult(0);
    }
}

internal sealed class FakeOutboxPublisher : IOutboxMessagePublisher
{
    public Func<Guid, string, string, Exception?>? PublishFailureFactory { get; init; }
    public List<(Guid MessageId, string Type, string PayloadJson)> Published { get; } = [];

    public Task PublishAsync(Guid messageId, string type, string payloadJson, CancellationToken cancellationToken)
    {
        Exception? exception = PublishFailureFactory?.Invoke(messageId, type, payloadJson);
        if (exception is not null)
            throw exception;

        Published.Add((messageId, type, payloadJson));
        return Task.CompletedTask;
    }
}

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "Matrix";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

using MediatR;

namespace Matrix.Education.Api.Tests.TestSupport
{
    internal sealed class EducationApiSenderStub : ISender
    {
        private readonly Dictionary<Type, Func<object, object?>> _handlers = [];

        internal List<object> Requests { get; } = [];

        internal void Handle<TRequest, TResponse>(Func<TRequest, TResponse> handler)
            where TRequest : notnull
        {
            _handlers[typeof(TRequest)] = request => handler((TRequest)request);
        }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (!_handlers.TryGetValue(request.GetType(), out Func<object, object?>? handler))
                throw new InvalidOperationException(
                    $"No API test handler is registered for '{request.GetType().Name}'.");

            return Task.FromResult((TResponse)handler(request)!);
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Requests.Add(request);
            if (!_handlers.TryGetValue(request.GetType(), out Func<object, object?>? handler))
                throw new InvalidOperationException(
                    $"No API test handler is registered for '{request.GetType().Name}'.");

            handler(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(
            object request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (!_handlers.TryGetValue(request.GetType(), out Func<object, object?>? handler))
                throw new InvalidOperationException(
                    $"No API test handler is registered for '{request.GetType().Name}'.");

            return Task.FromResult(handler(request));
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

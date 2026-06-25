using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using MediatR;

namespace Matrix.Healthcare.Integration.Tests.TestSupport
{
    internal sealed class HealthcareIntegrationMediatorStub : IMediator
    {
        internal List<SynchronizePatientProfilesCommand> Commands { get; } = [];

        internal SynchronizePatientProfilesResult Result { get; set; } = new(
            AddedProfiles: 0,
            UpdatedProfiles: 0,
            IgnoredProfiles: 0);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            if (request is not SynchronizePatientProfilesCommand command)
                throw new NotSupportedException(request.GetType().FullName);

            Commands.Add(command);
            return Task.FromResult((TResponse)(object)Result);
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            throw new NotSupportedException();
        }

        public Task<object?> Send(
            object request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish(
            object notification,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new NotSupportedException();
        }
    }
}

using System.Threading;

namespace Matrix.ApiGateway.Authorization.InternalJwt
{
    public sealed record InternalJwtRequestContext(
        Guid UserId,
        string? Jti,
        int PermissionsVersion,
        string[] EffectivePermissions);

    public interface IInternalJwtRequestContextAccessor
    {
        InternalJwtRequestContext? Current { get; }

        IDisposable Push(InternalJwtRequestContext context);
    }

    public sealed class InternalJwtRequestContextAccessor : IInternalJwtRequestContextAccessor
    {
        private readonly AsyncLocal<Scope?> _current = new();

        public InternalJwtRequestContext? Current => _current.Value?.Context;

        public IDisposable Push(InternalJwtRequestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            Scope? previous = _current.Value;
            _current.Value = new Scope(
                Context: context,
                Previous: previous);

            return new PopScope(
                owner: this,
                previous: previous);
        }

        private sealed record Scope(
            InternalJwtRequestContext Context,
            Scope? Previous);

        private sealed class PopScope(
            InternalJwtRequestContextAccessor owner,
            Scope? previous) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                owner._current.Value = previous;
                _disposed = true;
            }
        }
    }
}

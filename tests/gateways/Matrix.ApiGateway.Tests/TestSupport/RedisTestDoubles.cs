using System.Reflection;
using StackExchange.Redis;

namespace Matrix.ApiGateway.Tests.TestSupport
{
    public sealed class FakeRedisDatabaseState
    {
        private readonly Dictionary<string, string> _locks = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> _sets = new(StringComparer.Ordinal);

        public int GetDatabaseCallCount { get; private set; }
        public List<string> LockTakeKeys { get; } = [];
        public List<(string Key, string Token)> ReleasedLocks { get; } = [];

        internal void RecordGetDatabase()
        {
            GetDatabaseCallCount++;
        }

        public void SeedSet(
            string key,
            params string[] values)
        {
            _sets[key] = new HashSet<string>(
                collection: values,
                comparer: StringComparer.Ordinal);
        }

        public IReadOnlyCollection<string> GetSetMembers(string key)
        {
            return _sets.TryGetValue(
                key: key,
                value: out HashSet<string>? values)
                ? values.ToArray()
                : [];
        }

        public bool SetContains(
            string key,
            string value)
        {
            return _sets.TryGetValue(
                       key: key,
                       value: out HashSet<string>? values) &&
                   values.Contains(value);
        }

        internal RedisValue[] SetMembers(string key)
        {
            return _sets.TryGetValue(
                key: key,
                value: out HashSet<string>? values)
                ? values.Select(static value => (RedisValue)value)
                   .ToArray()
                : [];
        }

        internal bool SetAdd(
            string key,
            string value)
        {
            return GetOrCreateSet(key)
               .Add(value);
        }

        internal bool SetRemove(
            string key,
            string value)
        {
            return _sets.TryGetValue(
                       key: key,
                       value: out HashSet<string>? values) &&
                   values.Remove(value);
        }

        internal long SetRemoveMany(
            string key,
            IReadOnlyCollection<string> values)
        {
            if (!_sets.TryGetValue(
                    key: key,
                    value: out HashSet<string>? existing))
                return 0;

            long removed = 0;
            foreach (string value in values)
                if (existing.Remove(value))
                    removed++;

            return removed;
        }

        internal bool LockTake(
            string key,
            string token)
        {
            LockTakeKeys.Add(key);

            if (_locks.ContainsKey(key))
                return false;

            _locks[key] = token;
            return true;
        }

        internal bool LockRelease(
            string key,
            string token)
        {
            if (_locks.TryGetValue(
                    key: key,
                    value: out string? existingToken) &&
                string.Equals(
                    a: existingToken,
                    b: token,
                    comparisonType: StringComparison.Ordinal))
            {
                _locks.Remove(key);
                ReleasedLocks.Add((key, token));
                return true;
            }

            return false;
        }

        public bool HasLock(string key)
        {
            return _locks.ContainsKey(key);
        }

        public void SeedLock(
            string key,
            string token)
        {
            _locks[key] = token;
        }

        private HashSet<string> GetOrCreateSet(string key)
        {
            if (!_sets.TryGetValue(
                    key: key,
                    value: out HashSet<string>? values))
            {
                values = new HashSet<string>(StringComparer.Ordinal);
                _sets[key] = values;
            }

            return values;
        }
    }

    public static class RedisTestDoubles
    {
        public static IConnectionMultiplexer CreateConnectionMultiplexer(FakeRedisDatabaseState state)
        {
            IDatabase database = CreateDatabase(state);
            IConnectionMultiplexer multiplexer =
                DispatchProxy.Create<IConnectionMultiplexer, ConnectionMultiplexerProxy>();
            var proxy = (ConnectionMultiplexerProxy)multiplexer;
            proxy.State = state;
            proxy.Database = database;
            return multiplexer;
        }

        private static IDatabase CreateDatabase(FakeRedisDatabaseState state)
        {
            IDatabase database = DispatchProxy.Create<IDatabase, DatabaseProxy>();
            var proxy = (DatabaseProxy)database;
            proxy.State = state;
            return database;
        }

        private static string GetKey(object? value)
        {
            return ((RedisKey?)value)?.ToString() ??
                   throw new InvalidOperationException("Redis key is missing.");
        }

        private static string GetValue(object? value)
        {
            return ((RedisValue?)value)?.ToString() ??
                   throw new InvalidOperationException("Redis value is missing.");
        }

        private static object? GetDefaultReturnValue(Type returnType)
        {
            if (returnType == typeof(void))
                return null;

            if (returnType == typeof(Task))
                return Task.CompletedTask;

            if (returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                Type resultType = returnType.GetGenericArguments()[0];
                object? defaultValue = resultType.IsValueType
                    ? Activator.CreateInstance(resultType)
                    : null;

                return typeof(Task)
                   .GetMethod(nameof(Task.FromResult))!
                   .MakeGenericMethod(resultType)
                   .Invoke(
                        obj: null,
                        parameters: [defaultValue]);
            }

            return returnType.IsValueType
                ? Activator.CreateInstance(returnType)
                : null;
        }

        private class ConnectionMultiplexerProxy : DispatchProxy
        {
            public required FakeRedisDatabaseState State { get; set; }
            public required IDatabase Database { get; set; }

            protected override object? Invoke(
                MethodInfo? targetMethod,
                object?[]? args)
            {
                if (targetMethod is null)
                    throw new InvalidOperationException("Proxy target method is missing.");

                return targetMethod.Name switch
                {
                    nameof(IConnectionMultiplexer.GetDatabase) => GetDatabase(),
                    nameof(IDisposable.Dispose) => null,
                    _ => GetDefaultReturnValue(targetMethod.ReturnType)
                };
            }

            private IDatabase GetDatabase()
            {
                State.RecordGetDatabase();
                return Database;
            }
        }

        private class DatabaseProxy : DispatchProxy
        {
            public required FakeRedisDatabaseState State { get; set; }

            protected override object? Invoke(
                MethodInfo? targetMethod,
                object?[]? args)
            {
                if (targetMethod is null)
                    throw new InvalidOperationException("Proxy target method is missing.");

                args ??= [];
                return targetMethod.Name switch
                {
                    nameof(IDatabase.SetMembersAsync) => Task.FromResult(State.SetMembers(GetKey(args[0]))),
                    nameof(IDatabase.SetAddAsync) => Task.FromResult(
                        State.SetAdd(
                            key: GetKey(args[0]),
                            value: GetValue(args[1]))),
                    nameof(IDatabase.SetRemoveAsync) => HandleSetRemoveAsync(args),
                    nameof(IDatabase.LockTakeAsync) => Task.FromResult(
                        State.LockTake(
                            key: GetKey(args[0]),
                            token: GetValue(args[1]))),
                    nameof(IDatabase.LockReleaseAsync) => Task.FromResult(
                        State.LockRelease(
                            key: GetKey(args[0]),
                            token: GetValue(args[1]))),
                    _ => GetDefaultReturnValue(targetMethod.ReturnType)
                };
            }

            private object HandleSetRemoveAsync(object?[] args)
            {
                string key = GetKey(args[0]);

                return args[1] switch
                {
                    RedisValue value => Task.FromResult(
                        State.SetRemove(
                            key: key,
                            value: value.ToString())),
                    RedisValue[] values => Task.FromResult(
                        State.SetRemoveMany(
                            key: key,
                            values: values.Select(static value => value.ToString())
                               .ToArray())),
                    _ => throw new NotSupportedException("Unsupported SetRemoveAsync arguments.")
                };
            }
        }
    }
}

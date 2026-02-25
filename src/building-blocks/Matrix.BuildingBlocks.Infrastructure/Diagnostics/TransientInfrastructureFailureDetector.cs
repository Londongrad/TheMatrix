using System.Net.Sockets;

namespace Matrix.BuildingBlocks.Infrastructure.Diagnostics
{
    public static class TransientInfrastructureFailureDetector
    {
        private static readonly HashSet<string> TransientExceptionTypeNames =
        [
            "RabbitMQ.Client.Exceptions.AlreadyClosedException",
            "RabbitMQ.Client.Exceptions.BrokerUnreachableException",
            "RabbitMQ.Client.Exceptions.ConnectFailureException",
            "RabbitMQ.Client.Exceptions.OperationInterruptedException",
            "MassTransit.RabbitMqTransport.RabbitMqConnectionException",
            "Npgsql.NpgsqlException"
        ];

        public static bool IsTransient(Exception exception)
        {
            foreach (Exception current in Flatten(exception))
            {
                if (current is SocketException or IOException or TimeoutException or TaskCanceledException)
                    return true;

                if (TransientExceptionTypeNames.Contains(current.GetType().FullName ?? string.Empty))
                    return true;
            }

            return false;
        }

        private static IEnumerable<Exception> Flatten(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
                yield return current;
        }
    }
}

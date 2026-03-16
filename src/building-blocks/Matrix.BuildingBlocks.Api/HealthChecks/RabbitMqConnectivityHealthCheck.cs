using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Matrix.BuildingBlocks.Api.HealthChecks
{
    internal sealed class RabbitMqConnectivityHealthCheck(
        string host,
        int port,
        TimeSpan timeout)
        : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(timeout);

            using var socket = new Socket(
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp);

            try
            {
                await socket.ConnectAsync(
                    host: host,
                    port: port,
                    cancellationToken: linkedCts.Token);

                return HealthCheckResult.Healthy(
                    description: $"RabbitMQ TCP connectivity to {host}:{port} is healthy.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy(
                    description:
                    $"RabbitMQ TCP connectivity to {host}:{port} timed out after {timeout.TotalSeconds:0.#} seconds.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    description: $"RabbitMQ TCP connectivity to {host}:{port} failed.",
                    exception: ex);
            }
        }
    }
}

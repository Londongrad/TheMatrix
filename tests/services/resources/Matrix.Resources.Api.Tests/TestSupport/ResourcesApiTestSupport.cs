using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Matrix.Resources.Api.Tests.TestSupport
{
    public static class ResourcesApiTestSupport
    {
        private static readonly Guid DefaultCityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        private static readonly Guid DefaultDistrictId = Guid.Parse("28e9e9cc-f6fc-4955-a733-c938586b4858");

        private static readonly DateTimeOffset LastEvaluatedAtUtc = new(
            year: 2051,
            month: 7,
            day: 11,
            hour: 10,
            minute: 15,
            second: 0,
            offset: TimeSpan.Zero);

        public static CityStockpilesDto CreateCityStockpilesDto(
            Guid? cityId = null,
            bool emergencyRationingEnabled = false)
        {
            return new CityStockpilesDto(
                CityId: cityId ?? DefaultCityId,
                EffectiveTickId: 42,
                SupplyStressIndex: 0.37m,
                EmergencyRationingEnabled: emergencyRationingEnabled,
                LastEvaluatedAtUtc: LastEvaluatedAtUtc,
                PendingResupply: CreatePendingResupplyDto(),
                Fuel: CreateLineDto(
                    kind: "Fuel",
                    stockLevelIndex: 0.62m,
                    demandPressureIndex: 0.48m,
                    resupplyReadinessIndex: 0.75m,
                    shortageRiskIndex: 0.22m),
                Food: CreateLineDto(
                    kind: "Food",
                    stockLevelIndex: 0.73m,
                    demandPressureIndex: 0.31m,
                    resupplyReadinessIndex: 0.80m,
                    shortageRiskIndex: 0.18m),
                Medicine: CreateLineDto(
                    kind: "Medicine",
                    stockLevelIndex: 0.69m,
                    demandPressureIndex: 0.35m,
                    resupplyReadinessIndex: 0.71m,
                    shortageRiskIndex: 0.20m),
                SpareParts: CreateLineDto(
                    kind: "SpareParts",
                    stockLevelIndex: 0.58m,
                    demandPressureIndex: 0.54m,
                    resupplyReadinessIndex: 0.66m,
                    shortageRiskIndex: 0.29m),
                Filters: CreateLineDto(
                    kind: "Filters",
                    stockLevelIndex: 0.64m,
                    demandPressureIndex: 0.40m,
                    resupplyReadinessIndex: 0.70m,
                    shortageRiskIndex: 0.24m),
                EmergencyWater: CreateLineDto(
                    kind: "EmergencyWater",
                    stockLevelIndex: 0.67m,
                    demandPressureIndex: 0.38m,
                    resupplyReadinessIndex: 0.72m,
                    shortageRiskIndex: 0.21m));
        }

        public static DispatchCityResupplyResult CreateDispatchCityResupplyResult(
            DispatchCityResupplyStatus status = DispatchCityResupplyStatus.Scheduled,
            Guid? cityId = null)
        {
            return new DispatchCityResupplyResult(
                Status: status,
                CityId: cityId ?? DefaultCityId,
                RequestedIntensity: "High",
                BudgetAuthorizedIntensity: "Medium",
                AppliedIntensity: "Medium",
                PendingResupply: CreatePendingResupplyDto(
                    focus: "Fuel",
                    intensity: "Medium",
                    readyAtTickId: 45),
                BudgetPressureIndex: 0.34m,
                BudgetAuthorizationStatus: status == DispatchCityResupplyStatus.AuthorizationDenied
                    ? "Denied"
                    : "Approved",
                BudgetAuthorizationLevel: "Managed",
                BudgetAvailableAmount: 1800m,
                BudgetAuthorizedByEmergencyOverride: false,
                BudgetAuthorizationSummary: "Resupply authorization summary",
                SupplyStressIndex: 0.37m,
                FuelStockLevelIndex: 0.62m,
                FoodStockLevelIndex: 0.73m,
                EmergencyWaterStockLevelIndex: 0.67m);
        }

        public static IConfiguration BuildValidApiConfiguration()
        {
            Dictionary<string, string?> values = new()
            {
                ["ConnectionStrings:ResourcesDb"] =
                    "Host=localhost;Port=5432;Database=resources_tests;Username=postgres;Password=postgres",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "resources-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "resources-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300",
                ["RabbitMq:Host"] = "rabbitmq.test",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:EndpointHygiene:DiscardSkippedMessages"] = "true",
                ["DownstreamServices:Economy"] = "https://economy.test",
                ["DownstreamServices:SimulationCore"] = "https://simulationcore.test",
                ["DatabaseStartup:Enabled"] = "false"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }

        public static WebApplicationBuilder CreateBuilder(IConfiguration? configuration = null)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = "Development"
                });

            if (configuration is not null)
            {
                builder.Configuration.Sources.Clear();
                builder.Configuration.AddConfiguration(configuration);
            }

            return builder;
        }

        public static T AssertResult<T>(
            IResult result,
            int expectedStatusCode)
        {
            IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(
                expected: expectedStatusCode,
                actual: status.StatusCode);

            IValueHttpResult value = Assert.IsAssignableFrom<IValueHttpResult>(result);
            return Assert.IsType<T>(value.Value);
        }

        public static void AssertStatus(
            IResult result,
            int expectedStatusCode)
        {
            IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(
                expected: expectedStatusCode,
                actual: status.StatusCode);
        }

        private static CityStockpileLineDto CreateLineDto(
            string kind,
            decimal stockLevelIndex,
            decimal demandPressureIndex,
            decimal resupplyReadinessIndex,
            decimal shortageRiskIndex)
        {
            return new CityStockpileLineDto(
                Kind: kind,
                StockLevelIndex: stockLevelIndex,
                DemandPressureIndex: demandPressureIndex,
                ResupplyReadinessIndex: resupplyReadinessIndex,
                ShortageRiskIndex: shortageRiskIndex);
        }

        private static PendingResupplyDto CreatePendingResupplyDto(
            string focus = "All",
            string intensity = "High",
            long readyAtTickId = 44)
        {
            return new PendingResupplyDto(
                Focus: focus,
                Intensity: intensity,
                FocusDistrictId: DefaultDistrictId,
                ReadyAtTickId: readyAtTickId);
        }

        public sealed class FakeSender : IMediator
        {
            private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = new();

            public List<object> Requests { get; } = [];

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Requests.Add(request);

                if (!_handlers.TryGetValue(
                        key: request.GetType(),
                        value: out Func<object, CancellationToken, Task<object?>>? handler))
                    throw new InvalidOperationException(
                        $"No handler registered for request type '{request.GetType().Name}'.");

                return Invoke<TResponse>(
                    handler: handler,
                    request: request,
                    cancellationToken: cancellationToken);
            }

            public async Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                Requests.Add(request);

                if (!_handlers.TryGetValue(
                        key: request.GetType(),
                        value: out Func<object, CancellationToken, Task<object?>>? handler))
                    throw new InvalidOperationException(
                        $"No handler registered for request type '{request.GetType().Name}'.");

                await handler(
                    arg1: request,
                    arg2: cancellationToken);
            }

            public async Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                Requests.Add(request);

                if (!_handlers.TryGetValue(
                        key: request.GetType(),
                        value: out Func<object, CancellationToken, Task<object?>>? handler))
                    throw new InvalidOperationException(
                        $"No handler registered for request type '{request.GetType().Name}'.");

                return await handler(
                    arg1: request,
                    arg2: cancellationToken);
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
                return Task.CompletedTask;
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                return Task.CompletedTask;
            }

            public void Handle<TRequest, TResponse>(Func<TRequest, TResponse> handler)
                where TRequest : notnull
            {
                _handlers[typeof(TRequest)] = (
                    request,
                    _) => Task.FromResult<object?>(handler((TRequest)request));
            }

            public void Handle<TRequest>(Action<TRequest> handler)
                where TRequest : notnull
            {
                _handlers[typeof(TRequest)] = (
                    request,
                    _) =>
                {
                    handler((TRequest)request);
                    return Task.FromResult<object?>(Unit.Value);
                };
            }

            private static async Task<TResponse> Invoke<TResponse>(
                Func<object, CancellationToken, Task<object?>> handler,
                object request,
                CancellationToken cancellationToken)
            {
                object? result = await handler(
                    arg1: request,
                    arg2: cancellationToken);
                return (TResponse)result!;
            }
        }
    }
}

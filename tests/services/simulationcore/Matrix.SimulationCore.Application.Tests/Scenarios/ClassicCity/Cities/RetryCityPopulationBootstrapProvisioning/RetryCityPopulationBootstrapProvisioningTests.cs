using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RetryCityPopulationBootstrapProvisioning;
using MediatR;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RetryCityPopulationBootstrapProvisioning
{
    public sealed class RetryCityPopulationBootstrapProvisioningTests
    {
        private readonly RetryCityPopulationBootstrapProvisioningCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidValues_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new RetryCityPopulationBootstrapProvisioningCommand(
                    CityId: Guid.NewGuid(),
                    PlannedPeopleCountOverride: 25_000));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidValues_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new RetryCityPopulationBootstrapProvisioningCommand(
                    CityId: Guid.Empty,
                    PlannedPeopleCountOverride: 1_000_001));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "PlannedPeopleCountOverride");
        }

        [Fact]
        public async Task Handle_WhenRestartReturnsNotFound_ReturnsNotFound()
        {
            var mediator = new FakeMediator
            {
                Result = RestartCityPopulationBootstrapResult.NotFound()
            };
            var orchestrator = new FakeClassicCityProvisioningOrchestrator();
            var handler = new RetryCityPopulationBootstrapProvisioningCommandHandler(
                mediator: mediator,
                orchestrator: orchestrator);
            var command = new RetryCityPopulationBootstrapProvisioningCommand(
                CityId: Guid.NewGuid(),
                PlannedPeopleCountOverride: 25_000);

            RetryCityPopulationBootstrapProvisioningResult result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            RestartCityPopulationBootstrapCommand restartCommand =
                Assert.IsType<RestartCityPopulationBootstrapCommand>(mediator.RequestedRequest);
            Assert.Equal(
                expected: command.CityId,
                actual: restartCommand.CityId);
            Assert.Equal(
                expected: command.PlannedPeopleCountOverride,
                actual: restartCommand.PlannedPeopleCountOverride);
            Assert.Equal(
                expected: RetryCityPopulationBootstrapProvisioningStatus.NotFound,
                actual: result.Status);
            Assert.Null(result.Provisioning);
            Assert.Null(orchestrator.RequestedCityId);
        }

        [Fact]
        public async Task Handle_WhenRestartReturnsNotAllowed_ReturnsNotAllowed()
        {
            var mediator = new FakeMediator
            {
                Result = RestartCityPopulationBootstrapResult.NotAllowed()
            };
            var orchestrator = new FakeClassicCityProvisioningOrchestrator();
            var handler = new RetryCityPopulationBootstrapProvisioningCommandHandler(
                mediator: mediator,
                orchestrator: orchestrator);

            RetryCityPopulationBootstrapProvisioningResult result = await handler.Handle(
                request: new RetryCityPopulationBootstrapProvisioningCommand(
                    CityId: Guid.NewGuid(),
                    PlannedPeopleCountOverride: 25_000),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RetryCityPopulationBootstrapProvisioningStatus.NotAllowed,
                actual: result.Status);
            Assert.Null(result.Provisioning);
            Assert.Null(orchestrator.RequestedCityId);
        }

        [Fact]
        public async Task Handle_WhenRestartSucceeds_ReturnsAcceptedProvisioningView()
        {
            var cityId = Guid.NewGuid();
            var provisioning = new CityProvisioningModel(
                CityId: cityId,
                SimulationKind: "ClassicCity",
                PopulationBootstrap: new CityPopulationBootstrapModel(
                    OperationId: Guid.NewGuid(),
                    Status: "Provisioning",
                    PlannedPeopleCount: 25_000,
                    ResidentialCapacity: null,
                    Summary: null,
                    FailureCode: null),
                EconomyBootstrap: new CityEconomyBootstrapModel(
                    OperationId: Guid.NewGuid(),
                    Status: "Provisioning",
                    FailureCode: null,
                    UnitKind: null,
                    UnitCode: null,
                    UnitDisplayName: null,
                    UnitSymbol: null));
            var mediator = new FakeMediator
            {
                Result = RestartCityPopulationBootstrapResult.Restarted(
                    populationOperationId: Guid.NewGuid(),
                    economyOperationId: Guid.NewGuid(),
                    simulationKind: "ClassicCity")
            };
            var orchestrator = new FakeClassicCityProvisioningOrchestrator
            {
                Result = provisioning
            };
            var handler = new RetryCityPopulationBootstrapProvisioningCommandHandler(
                mediator: mediator,
                orchestrator: orchestrator);

            RetryCityPopulationBootstrapProvisioningResult result = await handler.Handle(
                request: new RetryCityPopulationBootstrapProvisioningCommand(
                    CityId: cityId,
                    PlannedPeopleCountOverride: 25_000),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: orchestrator.RequestedCityId);
            Assert.Equal(
                expected: RetryCityPopulationBootstrapProvisioningStatus.Accepted,
                actual: result.Status);
            Assert.Equal(
                expected: provisioning,
                actual: result.Provisioning);
        }

        private sealed class FakeMediator : IMediator
        {
            public object? RequestedRequest { get; private set; }
            public object? Result { get; init; }

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

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                RequestedRequest = request;
                return Task.FromResult((TResponse)Result!);
            }

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                RequestedRequest = request;
                return Task.CompletedTask;
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                RequestedRequest = request;
                return Task.FromResult(Result);
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                return Empty<TResponse>();
            }

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
            {
                return Empty<object?>();
            }

            private static async IAsyncEnumerable<T> Empty<T>()
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        private sealed class FakeClassicCityProvisioningOrchestrator : IClassicCityProvisioningOrchestrator
        {
            public Guid? RequestedCityId { get; private set; }
            public CityProvisioningModel? Result { get; init; }

            public Task<CityProvisioningModel> CreateAsync(
                CreateCityCommand request,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<CityProvisioningModel> GetProvisioningViewAsync(
                Guid cityId,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;
                return Task.FromResult(Result ?? throw new NotSupportedException());
            }

            public Task<CityProvisioningModel> ProvisionAsync(
                Guid cityId,
                string simulationKind,
                Guid populationBootstrapOperationId,
                Guid economyBootstrapOperationId,
                int? plannedPeopleCountOverride,
                Func<CancellationToken, Task>? heartbeatAsync,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }
    }
}

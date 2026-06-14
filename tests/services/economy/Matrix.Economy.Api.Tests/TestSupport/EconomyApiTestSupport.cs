using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.AuthorizeCityBudgetOperation;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Matrix.Economy.Api.Tests.TestSupport
{
    public static class EconomyApiTestSupport
    {
        public static BudgetSummaryDto CreateBudgetSummaryDto()
        {
            return new BudgetSummaryDto(
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Balance: new Money(10250.55m),
                TotalTaxIncome: new Money(2200.00m),
                TotalIncomeTaxIncome: new Money(900.00m),
                TotalSalesTaxIncome: new Money(700.00m),
                TotalDirectRevenue: new Money(600.00m),
                TotalCityExpenses: new Money(1800.00m),
                TotalRetailTurnover: new Money(5300.00m),
                TotalGrossPayroll: new Money(4200.00m),
                TotalNetPayroll: new Money(3500.00m));
        }

        public static CityOperationalBudgetPressureDto CreateOperationalPressureDto(Guid cityId)
        {
            return new CityOperationalBudgetPressureDto(
                CityId: cityId,
                EffectiveTickId: 42,
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Balance: 10250.55m,
                TotalCityExpenses: 1800.00m,
                MunicipalOperationsExpenses: 400.00m,
                InfrastructureOperationsExpenses: 750.00m,
                EmergencyOperationsExpenses: 120.00m,
                GeneralAvailableAmount: 6000.00m,
                OperationsAvailableAmount: 2500.00m,
                InfrastructureAvailableAmount: 1800.00m,
                HealthcareAvailableAmount: 900.00m,
                GeneralAuthorizationLevel: "Open",
                OperationsAuthorizationLevel: "Managed",
                InfrastructureAuthorizationLevel: "Managed",
                HealthcareAuthorizationLevel: "Protected",
                LastMunicipalExpenseAtUtc: "2048-06-01T09:10:00Z",
                PressureIndex: 0.37m);
        }

        public static CityBudgetOperationAuthorizationDto CreateBudgetOperationAuthorizationDto(Guid cityId)
        {
            return new CityBudgetOperationAuthorizationDto(
                CityId: cityId,
                Category: "Operations",
                OperationKind: "DrainageMaintenance",
                RequestedIntensity: "Elevated",
                ApprovedIntensity: "Baseline",
                Status: "Approved",
                AuthorizationLevel: "Managed",
                AvailableAmount: 2500.00m,
                EstimatedAmount: 180.00m,
                PressureIndex: 0.37m,
                EmergencyOverrideRequested: false,
                AuthorizedByEmergencyOverride: false,
                Summary: "Approved at baseline intensity.");
        }

        public static CityEconomyBootstrapResultDto CreateBootstrapResultDto(Guid cityId)
        {
            return new CityEconomyBootstrapResultDto(
                CityId: cityId,
                BudgetCreated: true,
                CreatedAllocations: 4,
                CreatedBusinesses: 12,
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C");
        }

        public static BudgetLedgerEntryDto CreateBudgetLedgerEntryDto(string title = "Tax income")
        {
            return new BudgetLedgerEntryDto(
                EntryId: Guid.Parse("077ccedb-7a17-4df9-b4ef-f415a477f4cc"),
                OccurredAtUtc: "2048-06-01T09:00:00Z",
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Kind: "Revenue",
                Category: "Taxation",
                Amount: 125.50m,
                Title: title,
                Description: "Recorded from API test",
                Source: "Manual",
                ReferenceCode: "rev-42");
        }

        public static CursorPagedResult<BudgetLedgerEntryDto> CreateBudgetLedgerFeed()
        {
            return new CursorPagedResult<BudgetLedgerEntryDto>(
                items: [CreateBudgetLedgerEntryDto()],
                pageSize: 50,
                nextCursor: "budget-next");
        }

        public static CityBudgetAllocationDto CreateBudgetAllocationDto(
            Guid cityId,
            string category = "Operations")
        {
            return new CityBudgetAllocationDto(
                AllocationId: Guid.Parse("93a0d412-6051-4821-b0d7-9297051d33c8"),
                CityId: cityId,
                Category: category,
                CreatedAtUtc: "2048-06-01T08:00:00Z",
                UpdatedAtUtc: "2048-06-01T09:00:00Z",
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                TargetAmount: 2400.00m,
                TotalSpent: 300.00m,
                AvailableAmount: 2100.00m);
        }

        public static RunCityMunicipalOperatingCycleResultDto CreateOperatingCycleResultDto(Guid cityId)
        {
            return new RunCityMunicipalOperatingCycleResultDto(
                CityId: cityId,
                AllocationCategoriesTouched: 3,
                ProviderPayments: 5,
                TotalDisbursedAmount: 750.00m);
        }

        public static CityBusinessDto CreateBusinessDto(Guid cityId)
        {
            return new CityBusinessDto(
                BusinessId: Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6"),
                CityId: cityId,
                CreatedAtUtc: "2048-06-01T08:00:00Z",
                Name: "North Works",
                Kind: "Manufacturer",
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Balance: 4200.00m,
                TaxReserve: 380.00m,
                TotalCapitalInjections: 2500.00m,
                TotalRetailTurnover: 900.00m,
                TotalNetSalesRevenue: 760.00m,
                TotalOperatingExpenses: 430.00m,
                TotalTaxRemitted: 120.00m);
        }

        public static CityBusinessLedgerEntryDto CreateBusinessLedgerEntryDto(string title = "Retail sale")
        {
            return new CityBusinessLedgerEntryDto(
                EntryId: Guid.Parse("d3abda8e-ac39-4ef1-af31-d34daf46f5d6"),
                OccurredAtUtc: "2048-06-01T09:05:00Z",
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Kind: "RetailSale",
                Amount: 210.00m,
                TaxAmount: 12.50m,
                Title: title,
                Description: "Recorded from API test",
                Source: "Manual",
                ReferenceCode: "biz-42");
        }

        public static CursorPagedResult<CityBusinessLedgerEntryDto> CreateBusinessLedgerFeed()
        {
            return new CursorPagedResult<CityBusinessLedgerEntryDto>(
                items: [CreateBusinessLedgerEntryDto()],
                pageSize: 25,
                nextCursor: "business-next");
        }

        public static RunCityBusinessTaxCycleResultDto CreateTaxCycleResultDto(Guid cityId)
        {
            return new RunCityBusinessTaxCycleResultDto(
                CityId: cityId,
                BudgetCategory: "Taxation",
                RemittedBusinesses: 4,
                TotalRemittedAmount: 450.00m);
        }

        public static CityHouseholdAccountDto CreateHouseholdAccountDto(Guid cityId)
        {
            return new CityHouseholdAccountDto(
                HouseholdAccountId: Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974"),
                CityId: cityId,
                CreatedAtUtc: "2048-06-01T08:00:00Z",
                Name: "Anderson Household",
                ExternalReferenceCode: "HH-01",
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Balance: 1200.00m,
                TotalOpeningBalance: 900.00m,
                TotalPayrollIncome: 600.00m,
                TotalConsumerSpending: 300.00m);
        }

        public static CityHouseholdAccountLedgerEntryDto CreateHouseholdAccountLedgerEntryDto(
            string title = "Household purchase")
        {
            return new CityHouseholdAccountLedgerEntryDto(
                EntryId: Guid.Parse("e71758d3-d3ad-40ae-8f42-b845ef7d2675"),
                OccurredAtUtc: "2048-06-01T09:15:00Z",
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Kind: "Purchase",
                Amount: 75.00m,
                Title: title,
                Description: "Recorded from API test",
                Source: "Manual",
                ReferenceCode: "acct-42");
        }

        public static CursorPagedResult<CityHouseholdAccountLedgerEntryDto> CreateHouseholdAccountLedgerFeed()
        {
            return new CursorPagedResult<CityHouseholdAccountLedgerEntryDto>(
                items: [CreateHouseholdAccountLedgerEntryDto()],
                pageSize: 20,
                nextCursor: "household-next");
        }

        public static CityHouseholdObligationDto CreateHouseholdObligationDto(Guid cityId)
        {
            return new CityHouseholdObligationDto(
                ObligationId: Guid.Parse("0d18f845-d6d4-41af-a4b8-c6dc2f9495dc"),
                CityId: cityId,
                HouseholdAccountId: Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974"),
                ProviderBusinessId: Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6"),
                CreatedAtUtc: "2048-06-01T08:20:00Z",
                Name: "Water Bill",
                Kind: "Utilities",
                IsActive: true,
                UnitKind: "Currency",
                UnitCode: "CRD",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                ChargeAmount: 40.00m,
                TaxAmount: 3.00m,
                BillingCadence: "Monthly",
                NextChargeDueAtUtc: "2048-07-01T00:00:00Z",
                LastChargedAtUtc: "2048-06-01T00:00:00Z",
                ChargeCount: 1);
        }

        public static RunCityHouseholdBillingCycleResultDto CreateBillingCycleResultDto(Guid cityId)
        {
            return new RunCityHouseholdBillingCycleResultDto(
                CityId: cityId,
                AsOfUtc: "2048-06-01T10:00:00Z",
                ChargedObligations: 6,
                TotalChargedAmount: 240.00m,
                TotalTaxAmount: 18.00m);
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

        public static IConfiguration BuildValidApiConfiguration()
        {
            Dictionary<string, string?> values = new()
            {
                ["ConnectionStrings:EconomyDb"] =
                    "Host=localhost;Port=5432;Database=economy_tests;Username=postgres;Password=postgres",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "economy-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "economy-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300",
                ["RabbitMq:Host"] = "rabbitmq.test",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:EndpointHygiene:DiscardSkippedMessages"] = "true",
                ["DatabaseStartup:Enabled"] = "false"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }

        public sealed class FakeSender : ISender
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

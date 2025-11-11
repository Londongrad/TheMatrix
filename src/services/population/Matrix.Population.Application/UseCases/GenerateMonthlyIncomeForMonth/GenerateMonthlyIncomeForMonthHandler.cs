using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.IntegrationEvents;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.UseCases.GenerateMonthlyIncomeForMonth
{
    public sealed class GenerateMonthlyIncomeForMonthHandler(
        IPersonReadRepository personRepository,
        IMonthlyIncomeEventPublisher eventPublisher,
        MonthlyIncomeCalculator calculator)
        : IRequestHandler<GenerateMonthlyIncomeForMonthCommand, Unit>
    {
        private readonly IPersonReadRepository _personRepository = personRepository;
        private readonly IMonthlyIncomeEventPublisher _eventPublisher = eventPublisher;
        private readonly MonthlyIncomeCalculator _calculator = calculator;

        public async Task<Unit> Handle(
            GenerateMonthlyIncomeForMonthCommand request,
            CancellationToken cancellationToken)
        {
            if (request.SimulationMonth <= 0)
                throw new DomainValidationException(
                    "SimulationMonth must be positive.",
                    nameof(request.SimulationMonth));

            var persons = await _personRepository.GetAllAsync(cancellationToken);

            var incomes = _calculator.CalculateMonthlyIncome(
                persons,
                request.SimulationMonth);

            foreach (var income in incomes)
            {
                var integrationEvent = new MonthlyIncomeEarnedIntegrationEvent(
                    EventId: Guid.NewGuid(),
                    OccurredAt: DateTimeOffset.UtcNow,
                    CorrelationId: Guid.NewGuid().ToString(),

                    PersonId: income.PersonId.Value,
                    HouseholdId: income.HouseholdId.Value,
                    DistrictId: income.DistrictId.Value,
                    WorkplaceId: income.WorkplaceId.Value,

                    GrossAmount: income.MonthlyIncome.Gross,
                    NetAmount: income.MonthlyIncome.Net,
                    TaxAmount: income.MonthlyIncome.Tax,

                    SimulationMonth: income.SimulationMonth);

                await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            }

            return Unit.Value;
        }
    }
}

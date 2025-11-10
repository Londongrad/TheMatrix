using MediatR;

namespace Matrix.Population.Application.UseCases.GenerateMonthlyIncomeForMonth
{
    public sealed record GenerateMonthlyIncomeForMonthCommand(int SimulationMonth) : IRequest<Unit>;
}

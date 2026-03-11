using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessExpense
{
    public sealed record RecordCityBusinessExpenseCommand(
        Guid BusinessId,
        decimal Amount,
        string Title,
        string? Description) : IRequest<CityBusinessLedgerEntryDto>;
}

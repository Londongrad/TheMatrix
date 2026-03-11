using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Models
{
    public sealed record CityBudgetUnitProfile(
        CityBudgetUnitKind Kind,
        string Code,
        string DisplayName,
        string Symbol)
    {
        public static CityBudgetUnitProfile DefaultMoney() => new(
            Kind: CityBudgetUnitKind.Currency,
            Code: "MNY",
            DisplayName: "Money",
            Symbol: "¤");
    }
}

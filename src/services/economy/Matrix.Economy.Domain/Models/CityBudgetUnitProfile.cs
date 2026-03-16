using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Models
{
    public sealed record CityBudgetUnitProfile(
        CityBudgetUnitKind Kind,
        string Code,
        string DisplayName,
        string Symbol)
    {
        public static CityBudgetUnitProfile DefaultMoney()
        {
            return new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: "MNY",
                DisplayName: "Money",
                Symbol: "¤");
        }
    }
}

using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.Economy.Contracts.Scenarios.ClassicCity.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string EconomyService = "Economy";

        private const string BudgetGroup = "Budget";
        private const string BusinessesGroup = "Businesses";
        private const string HouseholdAccountsGroup = "Household Accounts";
        private const string HouseholdObligationsGroup = "Household Obligations";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new(
                    Key: PermissionKeys.EconomyBudgetRead,
                    Service: EconomyService,
                    Group: BudgetGroup,
                    Description: "View city budget summaries, ledgers, allocations, and operational pressure."),
                new(
                    Key: PermissionKeys.EconomyBudgetManage,
                    Service: EconomyService,
                    Group: BudgetGroup,
                    Description: "Record budget entries, set allocations, disburse funds, and run municipal cycles."),
                new(
                    Key: PermissionKeys.EconomyBudgetBootstrap,
                    Service: EconomyService,
                    Group: BudgetGroup,
                    Description: "Initialize economy state for newly created cities."),
                new(
                    Key: PermissionKeys.EconomyBudgetAuthorize,
                    Service: EconomyService,
                    Group: BudgetGroup,
                    Description: "Authorize live budget-controlled operations for downstream services."),
                new(
                    Key: PermissionKeys.EconomyBusinessesRead,
                    Service: EconomyService,
                    Group: BusinessesGroup,
                    Description: "View city businesses and business ledgers."),
                new(
                    Key: PermissionKeys.EconomyBusinessesManage,
                    Service: EconomyService,
                    Group: BusinessesGroup,
                    Description: "Register businesses, record business activity, and run tax operations."),
                new(
                    Key: PermissionKeys.EconomyHouseholdAccountsRead,
                    Service: EconomyService,
                    Group: HouseholdAccountsGroup,
                    Description: "View household accounts and household account ledgers."),
                new(
                    Key: PermissionKeys.EconomyHouseholdAccountsManage,
                    Service: EconomyService,
                    Group: HouseholdAccountsGroup,
                    Description: "Register household accounts and record household purchases."),
                new(
                    Key: PermissionKeys.EconomyHouseholdObligationsRead,
                    Service: EconomyService,
                    Group: HouseholdObligationsGroup,
                    Description: "View household obligations across a city."),
                new(
                    Key: PermissionKeys.EconomyHouseholdObligationsManage,
                    Service: EconomyService,
                    Group: HouseholdObligationsGroup,
                    Description: "Register obligations, issue charges, and run household billing cycles.")
            };
    }
}

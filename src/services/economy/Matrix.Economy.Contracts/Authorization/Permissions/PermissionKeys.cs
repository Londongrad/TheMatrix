namespace Matrix.Economy.Contracts.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string EconomyBudgetRead = "economy.budget.read";
        public const string EconomyBudgetManage = "economy.budget.manage";
        public const string EconomyBudgetBootstrap = "economy.budget.bootstrap";
        public const string EconomyBudgetAuthorize = "economy.budget.authorize";

        public const string EconomyBusinessesRead = "economy.businesses.read";
        public const string EconomyBusinessesManage = "economy.businesses.manage";

        public const string EconomyHouseholdAccountsRead = "economy.household-accounts.read";
        public const string EconomyHouseholdAccountsManage = "economy.household-accounts.manage";

        public const string EconomyHouseholdObligationsRead = "economy.household-obligations.read";
        public const string EconomyHouseholdObligationsManage = "economy.household-obligations.manage";
    }
}

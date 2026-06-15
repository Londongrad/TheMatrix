using ContractPermissionKeys = Matrix.Economy.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string EconomyBudgetRead = ContractPermissionKeys.EconomyBudgetRead;
        public const string EconomyBudgetManage = ContractPermissionKeys.EconomyBudgetManage;
        public const string EconomyBudgetBootstrap = ContractPermissionKeys.EconomyBudgetBootstrap;
        public const string EconomyBudgetAuthorize = ContractPermissionKeys.EconomyBudgetAuthorize;

        public const string EconomyBusinessesRead = ContractPermissionKeys.EconomyBusinessesRead;
        public const string EconomyBusinessesManage = ContractPermissionKeys.EconomyBusinessesManage;

        public const string EconomyHouseholdAccountsRead = ContractPermissionKeys.EconomyHouseholdAccountsRead;
        public const string EconomyHouseholdAccountsManage = ContractPermissionKeys.EconomyHouseholdAccountsManage;

        public const string EconomyHouseholdObligationsRead = ContractPermissionKeys.EconomyHouseholdObligationsRead;

        public const string EconomyHouseholdObligationsManage =
            ContractPermissionKeys.EconomyHouseholdObligationsManage;
    }
}

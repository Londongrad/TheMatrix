using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.Education.Contracts.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            new(
                Key: PermissionKeys.EducationEnrollmentsManage,
                Service: "Education",
                Group: "Enrollments",
                Description: "Manage student enrollment, completion, and withdrawal.")
        ];
    }
}

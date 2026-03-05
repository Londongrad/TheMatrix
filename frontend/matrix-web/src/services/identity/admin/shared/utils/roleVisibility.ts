type NamedRole = {
    name: string;
};

const HIDDEN_ADMIN_ROLE_NAMES = new Set(["SuperAdmin"]);

export function isHiddenAdminRole(roleOrName: NamedRole | string) {
    const roleName = typeof roleOrName === "string" ? roleOrName : roleOrName.name;
    return HIDDEN_ADMIN_ROLE_NAMES.has(roleName);
}

export function filterVisibleAdminRoles<T extends NamedRole>(roles: T[]) {
    return roles.filter((role) => !isHiddenAdminRole(role));
}

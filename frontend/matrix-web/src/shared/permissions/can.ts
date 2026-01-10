export const can = (permissions: string[], permission: string): boolean =>
  permissions.includes(permission);

export const canAny = (permissions: string[], perms: string[]): boolean =>
  perms.some((perm) => permissions.includes(perm));

export const canAll = (permissions: string[], perms: string[]): boolean =>
  perms.every((perm) => permissions.includes(perm));

// src/services/identity/api/admin/adminTypes.ts
export interface UserListItemResponse {
  id: string;
  avatarUrl?: string | null;
  email: string;
  username: string;
  isEmailConfirmed: boolean;
  isLocked: boolean;
  createdAtUtc: string;
}

export interface UserDetailsResponse {
  id: string;
  avatarUrl?: string | null;
  username: string;
  email: string;
  isEmailConfirmed: boolean;
  isLocked: boolean;
  permissionsVersion: number;
  createdAtUtc: string;
}

export interface UserRoleResponse {
  id: string;
  name: string;
  isSystem: boolean;
  createdAtUtc: string;
}

export interface RoleResponse {
  id: string;
  name: string;
  isSystem: boolean;
  createdAtUtc: string;
}

export interface CreateRoleRequest {
  name: string;
}

export interface RenameRoleRequest {
  name: string;
}

export interface RolePermissionsResponse {
  permissionKeys: string[];
}

export type PermissionEffect = "Allow" | "Deny";
export interface UserPermissionResponse {
  permissionKey: string;
  effect: PermissionEffect;
}

export interface PermissionCatalogItemResponse {
  key: string;
  service: string;
  group: string;
  description: string;
  isDeprecated: boolean;
}

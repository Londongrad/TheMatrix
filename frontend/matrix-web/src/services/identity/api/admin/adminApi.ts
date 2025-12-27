// src/services/identity/api/admin/adminApi.ts
import { apiRequest } from "@shared/api/http";
import { API_ADMIN_URL, API_ADMIN_USERS_URL } from "@shared/api/config";
import type {
  PagedResult,
  PermissionCatalogItemResponse,
  RoleResponse,
  UserDetailsResponse,
  UserListItemResponse,
  UserPermissionResponse,
  UserRoleResponse,
} from "./adminTypes";

export async function getUsersPage(
  pageNumber: number,
  pageSize: number
): Promise<PagedResult<UserListItemResponse>> {
  return await apiRequest<PagedResult<UserListItemResponse>>(
    `${API_ADMIN_USERS_URL}?pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
}

export async function getUserDetails(
  userId: string
): Promise<UserDetailsResponse> {
  return await apiRequest<UserDetailsResponse>(
    `${API_ADMIN_USERS_URL}/${userId}`
  );
}

export async function lockUser(userId: string): Promise<void> {
  await apiRequest<void>(`${API_ADMIN_USERS_URL}/${userId}/lock`, {
    method: "POST",
  });
}

export async function unlockUser(userId: string): Promise<void> {
  await apiRequest<void>(`${API_ADMIN_USERS_URL}/${userId}/unlock`, {
    method: "POST",
  });
}

export async function getUserRoles(
  userId: string
): Promise<UserRoleResponse[]> {
  return await apiRequest<UserRoleResponse[]>(
    `${API_ADMIN_USERS_URL}/${userId}/roles`
  );
}

export async function assignUserRoles(
  userId: string,
  roleIds: string[]
): Promise<void> {
  await apiRequest<void>(`${API_ADMIN_USERS_URL}/${userId}/roles`, {
    method: "PUT",
    body: JSON.stringify({ roleIds }),
  });
}

export async function getUserPermissions(
  userId: string
): Promise<UserPermissionResponse[]> {
  return await apiRequest<UserPermissionResponse[]>(
    `${API_ADMIN_USERS_URL}/${userId}/permissions`
  );
}

export async function grantUserPermission(
  userId: string,
  permissionKey: string
): Promise<void> {
  await apiRequest<void>(`${API_ADMIN_USERS_URL}/${userId}/permissions/grant`, {
    method: "POST",
    body: JSON.stringify({ permissionKey }),
  });
}

export async function depriveUserPermission(
  userId: string,
  permissionKey: string
): Promise<void> {
  await apiRequest<void>(
    `${API_ADMIN_USERS_URL}/${userId}/permissions/deprive`,
    {
      method: "POST",
      body: JSON.stringify({ permissionKey }),
    }
  );
}

export async function getRolesCatalog(): Promise<RoleResponse[]> {
  return await apiRequest<RoleResponse[]>(`${API_ADMIN_URL}/roles`);
}

export async function getPermissionsCatalog(): Promise<
  PermissionCatalogItemResponse[]
> {
  return await apiRequest<PermissionCatalogItemResponse[]>(
    `${API_ADMIN_URL}/permissions`
  );
}

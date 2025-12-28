// src/services/identity/admin/pages/AdminUserPage.tsx
import { useEffect, useMemo, useState } from "react";
import { useAuth } from "@services/identity/api/auth/AuthContext";
import {
  assignUserRoles,
  depriveUserPermission,
  getPermissionsCatalog,
  getRolesCatalog,
  getUserDetails,
  getUserPermissions,
  getUserRoles,
  getUsersPage,
  grantUserPermission,
  lockUser,
  unlockUser,
} from "@services/identity/api/admin/adminApi";
import type {
  PermissionCatalogItemResponse,
  RoleResponse,
  UserDetailsResponse,
  UserListItemResponse,
  UserPermissionResponse,
  UserRoleResponse,
} from "@services/identity/api/admin/adminTypes";
import "@services/identity/admin/styles/admin-users-page.css";

const PAGE_SIZE = 25;

const AdminUsersPage = () => {
  const { token } = useAuth();

  const [users, setUsers] = useState<UserListItemResponse[]>([]);
  const [totalUsers, setTotalUsers] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  const [usersError, setUsersError] = useState<string | null>(null);

  const [rolesCatalog, setRolesCatalog] = useState<RoleResponse[]>([]);
  const [permissionsCatalog, setPermissionsCatalog] = useState<
    PermissionCatalogItemResponse[]
  >([]);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [isLoadingCatalog, setIsLoadingCatalog] = useState(false);

  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<UserDetailsResponse | null>(
    null
  );
  const [userRoles, setUserRoles] = useState<UserRoleResponse[]>([]);
  const [userPermissions, setUserPermissions] = useState<
    UserPermissionResponse[]
  >([]);
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);
  const [detailsError, setDetailsError] = useState<string | null>(null);

  const [roleSaveState, setRoleSaveState] = useState<
    "idle" | "saving" | "success" | "error"
  >("idle");
  const [permissionActionState, setPermissionActionState] = useState<
    "idle" | "saving" | "error"
  >("idle");
  const [permissionFilter, setPermissionFilter] = useState("");

  const totalPages = Math.max(1, Math.ceil(totalUsers / PAGE_SIZE));

  useEffect(() => {
    if (!token) {
      setUsers([]);
      setTotalUsers(0);
      setPageNumber(1);
      setUsersError(null);
      setIsLoadingUsers(false);
      return;
    }

    let isActual = true;

    (async () => {
      try {
        setIsLoadingUsers(true);
        setUsersError(null);

        const result = await getUsersPage(pageNumber, PAGE_SIZE);

        if (!isActual) return;
        setUsers(result.items);
        setTotalUsers(result.totalCount);
      } catch (error) {
        if (!isActual) return;
        console.error(error);
        setUsersError("Failed to load users.");
      } finally {
        if (!isActual) return;
        setIsLoadingUsers(false);
      }
    })();

    return () => {
      isActual = false;
    };
  }, [pageNumber, token]);

  useEffect(() => {
    if (!token) {
      setRolesCatalog([]);
      setPermissionsCatalog([]);
      setCatalogError(null);
      setIsLoadingCatalog(false);
      return;
    }

    let isActual = true;

    (async () => {
      try {
        setIsLoadingCatalog(true);
        setCatalogError(null);

        const [roles, permissions] = await Promise.all([
          getRolesCatalog(),
          getPermissionsCatalog(),
        ]);

        if (!isActual) return;
        setRolesCatalog(roles);
        setPermissionsCatalog(permissions);
      } catch (error) {
        if (!isActual) return;
        console.error(error);
        setCatalogError("Failed to load roles & permissions catalog.");
      } finally {
        if (!isActual) return;
        setIsLoadingCatalog(false);
      }
    })();

    return () => {
      isActual = false;
    };
  }, [token]);

  useEffect(() => {
    if (!token) {
      setSelectedUserId(null);
      setSelectedUser(null);
      setUserRoles([]);
      setUserPermissions([]);
      setSelectedRoleIds([]);
      setDetailsError(null);
      setIsLoadingDetails(false);
      return;
    }

    if (!selectedUserId) {
      setSelectedUser(null);
      setUserRoles([]);
      setUserPermissions([]);
      setSelectedRoleIds([]);
      setDetailsError(null);
      setIsLoadingDetails(false);
      return;
    }

    let isActual = true;

    (async () => {
      try {
        setIsLoadingDetails(true);
        setDetailsError(null);

        const [details, roles, permissions] = await Promise.all([
          getUserDetails(selectedUserId),
          getUserRoles(selectedUserId),
          getUserPermissions(selectedUserId),
        ]);

        if (!isActual) return;
        setSelectedUser(details);
        setUserRoles(roles);
        setUserPermissions(permissions);
        setSelectedRoleIds(roles.map((role) => role.id));
      } catch (error) {
        if (!isActual) return;
        console.error(error);
        setDetailsError("Failed to load user details.");
      } finally {
        if (!isActual) return;
        setIsLoadingDetails(false);
      }
    })();

    return () => {
      isActual = false;
    };
  }, [selectedUserId, token]);

  const filteredPermissions = useMemo(() => {
    const query = permissionFilter.trim().toLowerCase();

    const matches = (permission: PermissionCatalogItemResponse) => {
      if (!query) return true;
      return (
        permission.key.toLowerCase().includes(query) ||
        permission.description.toLowerCase().includes(query) ||
        permission.service.toLowerCase().includes(query) ||
        permission.group.toLowerCase().includes(query)
      );
    };

    return permissionsCatalog.filter(matches).slice(0, 60);
  }, [permissionFilter, permissionsCatalog]);

  const handleSelectUser = (userId: string) => {
    setSelectedUserId(userId);
  };

  const handleToggleRole = (roleId: string) => {
    setSelectedRoleIds((prev) =>
      prev.includes(roleId)
        ? prev.filter((id) => id !== roleId)
        : [...prev, roleId]
    );
    setRoleSaveState("idle");
  };

  const handleSaveRoles = async () => {
    if (!selectedUserId) return;

    try {
      setRoleSaveState("saving");
      await assignUserRoles(selectedUserId, selectedRoleIds);
      const updatedRoles = await getUserRoles(selectedUserId);
      setUserRoles(updatedRoles);
      setSelectedRoleIds(updatedRoles.map((role) => role.id));
      setRoleSaveState("success");
    } catch (error) {
      console.error(error);
      setRoleSaveState("error");
    }
  };

  const handleLockToggle = async () => {
    if (!selectedUser) return;

    try {
      if (selectedUser.isLocked) {
        await unlockUser(selectedUser.id);
      } else {
        await lockUser(selectedUser.id);
      }

      const refreshed = await getUserDetails(selectedUser.id);
      setSelectedUser(refreshed);
      setUsers((prev) =>
        prev.map((item) =>
          item.id === refreshed.id
            ? { ...item, isLocked: refreshed.isLocked }
            : item
        )
      );
    } catch (error) {
      console.error(error);
      setDetailsError("Failed to update lock status.");
    }
  };

  const handlePermissionAction = async (
    permissionKey: string,
    action: "grant" | "deprive"
  ) => {
    if (!selectedUserId) return;

    try {
      setPermissionActionState("saving");

      if (action === "grant") {
        await grantUserPermission(selectedUserId, permissionKey);
      } else {
        await depriveUserPermission(selectedUserId, permissionKey);
      }

      const updatedPermissions = await getUserPermissions(selectedUserId);
      setUserPermissions(updatedPermissions);
      setPermissionActionState("idle");
    } catch (error) {
      console.error(error);
      setPermissionActionState("error");
    }
  };

  const permissionEffectLookup = useMemo(() => {
    const map = new Map<string, string>();
    userPermissions.forEach((permission) => {
      map.set(permission.permissionKey, permission.effect);
    });
    return map;
  }, [userPermissions]);

  return (
    <div className="admin-users-page">
      <div className="admin-users-header">
        <div>
          <h1 className="page-title">User administration</h1>
          <p className="card-sub">
            Manage user access, assign roles, and grant permissions.
          </p>
        </div>
        {isLoadingCatalog && (
          <span className="card-sub admin-users-inline">Loading catalog…</span>
        )}
        {catalogError && <span className="error-text">{catalogError}</span>}
      </div>

      <div className="admin-users-grid">
        <section className="admin-users-panel">
          <div className="admin-panel-header">
            <div>
              <h2 className="admin-panel-title">Users</h2>
              <span className="card-sub">
                {totalUsers > 0
                  ? `Showing page ${pageNumber} of ${totalPages}`
                  : "No users found."}
              </span>
            </div>
            <div className="admin-panel-actions">
              <button
                className="btn btn-sm"
                onClick={() => setPageNumber((prev) => Math.max(1, prev - 1))}
                disabled={pageNumber <= 1 || isLoadingUsers}
              >
                Prev
              </button>
              <button
                className="btn btn-sm"
                onClick={() =>
                  setPageNumber((prev) => Math.min(totalPages, prev + 1))
                }
                disabled={pageNumber >= totalPages || isLoadingUsers}
              >
                Next
              </button>
            </div>
          </div>

          {usersError && <p className="error-text">{usersError}</p>}

          {isLoadingUsers && (
            <p className="card-sub admin-users-inline">Loading users…</p>
          )}

          <div className="admin-users-list">
            {users.map((user) => {
              const isActive = user.id === selectedUserId;
              return (
                <button
                  key={user.id}
                  className={`admin-user-row ${
                    isActive ? "admin-user-row--active" : ""
                  }`}
                  onClick={() => handleSelectUser(user.id)}
                >
                  <div className="admin-user-avatar">
                    {user.avatarUrl ? (
                      <img src={user.avatarUrl} alt={user.username} />
                    ) : (
                      <span>{user.username.slice(0, 1).toUpperCase()}</span>
                    )}
                  </div>
                  <div className="admin-user-meta">
                    <div className="admin-user-title">
                      <span>{user.username}</span>
                      {user.isLocked && (
                        <span className="admin-user-tag admin-user-tag--danger">
                          Locked
                        </span>
                      )}
                      {!user.isEmailConfirmed && (
                        <span className="admin-user-tag admin-user-tag--warning">
                          Unconfirmed
                        </span>
                      )}
                    </div>
                    <span className="card-sub">{user.email}</span>
                  </div>
                </button>
              );
            })}
          </div>
        </section>

        <section className="admin-users-panel admin-users-panel--details">
          <div className="admin-panel-header">
            <div>
              <h2 className="admin-panel-title">User details</h2>
              <span className="card-sub">
                {selectedUser
                  ? `Created ${new Date(
                      selectedUser.createdAtUtc
                    ).toLocaleString()}`
                  : "Select a user from the list"}
              </span>
            </div>
            {selectedUser && (
              <button
                className={`btn btn-sm ${
                  selectedUser.isLocked ? "btn-primary" : "btn-danger"
                }`}
                onClick={handleLockToggle}
                disabled={isLoadingDetails}
              >
                {selectedUser.isLocked ? "Unlock" : "Lock"}
              </button>
            )}
          </div>

          {detailsError && <p className="error-text">{detailsError}</p>}
          {isLoadingDetails && (
            <p className="card-sub admin-users-inline">Loading details…</p>
          )}

          {!selectedUser && !isLoadingDetails && (
            <p className="card-sub">
              Select a user to view roles and permissions.
            </p>
          )}

          {selectedUser && !isLoadingDetails && (
            <div className="admin-user-details">
              <div className="admin-user-summary">
                <div className="admin-user-avatar admin-user-avatar--large">
                  {selectedUser.avatarUrl ? (
                    <img
                      src={selectedUser.avatarUrl}
                      alt={selectedUser.username}
                    />
                  ) : (
                    <span>
                      {selectedUser.username.slice(0, 1).toUpperCase()}
                    </span>
                  )}
                </div>
                <div>
                  <h3 className="admin-user-name">{selectedUser.username}</h3>
                  <p className="card-sub">{selectedUser.email}</p>
                  <p className="card-sub">
                    Permissions version: {selectedUser.permissionsVersion}
                  </p>
                </div>
              </div>

              <div className="admin-user-section">
                <div className="admin-section-header">
                  <h3>Roles</h3>
                  <button
                    className="btn btn-sm btn-primary"
                    onClick={handleSaveRoles}
                    disabled={roleSaveState === "saving"}
                  >
                    {roleSaveState === "saving" ? "Saving..." : "Save roles"}
                  </button>
                </div>

                <div className="admin-roles-grid">
                  {rolesCatalog.map((role) => (
                    <label key={role.id} className="admin-role-item">
                      <input
                        type="checkbox"
                        checked={selectedRoleIds.includes(role.id)}
                        onChange={() => handleToggleRole(role.id)}
                      />
                      <span>{role.name}</span>
                      {role.isSystem && (
                        <span className="admin-user-tag">System</span>
                      )}
                    </label>
                  ))}
                </div>

                {roleSaveState === "success" && (
                  <span className="card-sub admin-users-inline">
                    Roles updated.
                  </span>
                )}
                {roleSaveState === "error" && (
                  <span className="error-text">Failed to save roles.</span>
                )}
              </div>

              <div className="admin-user-section">
                <div className="admin-section-header">
                  <h3>Direct permissions</h3>
                  <span className="card-sub">
                    {userPermissions.length > 0
                      ? `Overrides: ${userPermissions.length}`
                      : "No direct overrides"}
                  </span>
                </div>

                <div className="admin-permissions-tags">
                  {userPermissions.map((permission) => (
                    <span
                      key={permission.permissionKey}
                      className={`admin-user-tag ${
                        permission.effect.toLowerCase() === "deny"
                          ? "admin-user-tag--danger"
                          : "admin-user-tag--success"
                      }`}
                    >
                      {permission.permissionKey} · {permission.effect}
                    </span>
                  ))}
                </div>
              </div>

              <div className="admin-user-section">
                <div className="admin-section-header">
                  <h3>Permissions catalog</h3>
                  <input
                    className="admin-search"
                    type="text"
                    placeholder="Filter by key, service, or description"
                    value={permissionFilter}
                    onChange={(event) =>
                      setPermissionFilter(event.target.value)
                    }
                  />
                </div>

                {permissionActionState === "error" && (
                  <span className="error-text">
                    Failed to update permission.
                  </span>
                )}

                <div className="admin-permissions-list">
                  {filteredPermissions.map((permission) => {
                    const effect = permissionEffectLookup.get(permission.key);
                    return (
                      <div
                        key={permission.key}
                        className="admin-permission-row"
                      >
                        <div>
                          <div className="admin-permission-key">
                            {permission.key}
                          </div>
                          <div className="card-sub">
                            {permission.description ||
                              `${permission.service} · ${permission.group}`}
                          </div>
                          {permission.isDeprecated && (
                            <span className="admin-user-tag admin-user-tag--warning">
                              Deprecated
                            </span>
                          )}
                          {effect && (
                            <span
                              className={`admin-user-tag ${
                                effect.toLowerCase() === "deny"
                                  ? "admin-user-tag--danger"
                                  : "admin-user-tag--success"
                              }`}
                            >
                              {effect}
                            </span>
                          )}
                        </div>
                        <div className="admin-permission-actions">
                          <button
                            className="btn btn-sm btn-primary"
                            onClick={() =>
                              handlePermissionAction(permission.key, "grant")
                            }
                            disabled={
                              permissionActionState === "saving" ||
                              effect?.toLowerCase() === "allow"
                            }
                          >
                            Grant
                          </button>
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() =>
                              handlePermissionAction(permission.key, "deprive")
                            }
                            disabled={
                              permissionActionState === "saving" ||
                              effect?.toLowerCase() === "deny"
                            }
                          >
                            Deprive
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
};

export default AdminUsersPage;
